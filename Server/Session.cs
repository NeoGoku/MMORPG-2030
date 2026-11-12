using System.Net.WebSockets;
using System.Text.Json;

namespace MMORPG.Server;

public static class Session
{
    public static async Task Run(HttpContext context, World world)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }
        var id = world.Join();
        if (id is null) { context.Response.StatusCode = 503; return; }
        try
        {
            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            await Send(socket, new { type = "welcome", version = 1, id, tickRate = 20 }, lifetime.Token);
            var receive = Receive(socket, world, id, lifetime.Token);
            var send = Snapshots(socket, world, lifetime.Token);
            await Task.WhenAny(receive, send);
            lifetime.Cancel();
            try { await Task.WhenAll(receive, send); }
            catch (Exception e) when (e is OperationCanceledException or WebSocketException or JsonException or InvalidDataException) { }
            // Bound close handshake; a vanished or malicious peer cannot retain a slot.
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                using var close = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session ended", close.Token); }
                catch (Exception e) when (e is OperationCanceledException or WebSocketException) { }
            }
        }
        finally { world.Leave(id); }
    }

    private static async Task Send(WebSocket socket, object value, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(value).AsMemory(), WebSocketMessageType.Text, true, timeout.Token);
    }

    private static async Task Snapshots(WebSocket socket, World world, CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        while (await timer.WaitForNextTickAsync(token)) await Send(socket, world.Snapshot(), token);
    }

    private static async Task Receive(WebSocket socket, World world, string id, CancellationToken token)
    {
        var buffer = new byte[4097];
        var window = DateTime.UtcNow;
        var count = 0;
        while (!token.IsCancellationRequested)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));
            var used = 0;
            ValueWebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer.AsMemory(used), timeout.Token);
                if (result.MessageType == WebSocketMessageType.Close) return;
                used += result.Count;
                if (result.MessageType != WebSocketMessageType.Text || used > 4096)
                    throw new InvalidDataException("Text messages up to 4096 bytes only");
            } while (!result.EndOfMessage);
            if ((DateTime.UtcNow - window).TotalSeconds >= 1) { window = DateTime.UtcNow; count = 0; }
            if (++count > 60) throw new InvalidDataException("Input rate exceeded");
            using var doc = JsonDocument.Parse(buffer.AsMemory(0, used));
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String || type.GetString() != "input" ||
                !root.TryGetProperty("seq", out var seq) || seq.ValueKind != JsonValueKind.Number || !seq.TryGetInt64(out var sequence) || sequence < 0 ||
                !root.TryGetProperty("x", out var x) || x.ValueKind != JsonValueKind.Number || !x.TryGetDouble(out var dx) || !double.IsFinite(dx) || Math.Abs(dx) > 1000 ||
                !root.TryGetProperty("z", out var z) || z.ValueKind != JsonValueKind.Number || !z.TryGetDouble(out var dz) || !double.IsFinite(dz) || Math.Abs(dz) > 1000)
                throw new InvalidDataException("Invalid input");
            world.Input(id, sequence, dx, dz);
        }
    }
}
