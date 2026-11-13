using System.Net.WebSockets;
using System.Text.Json;

namespace MMORPG.Server;

public static class Session
{
    public static async Task Run(HttpContext context, World world, AccountService auth, bool allowGuest)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }
        string? id = null;
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        try
        {
            if(allowGuest) id=world.Join();
            else {
                using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var bytes=new byte[4096];
                try {
                    var first=await socket.ReceiveAsync(bytes.AsMemory(),deadline.Token);
                    if(first.MessageType!=WebSocketMessageType.Text||!first.EndOfMessage)return;
                    using var packet=JsonDocument.Parse(bytes.AsMemory(0,first.Count));
                    if(packet.RootElement.TryGetProperty("ticket",out var ticket)&&ticket.ValueKind==JsonValueKind.String) {
                        var record=auth.ConsumeTicket(ticket.GetString()!);
                        if(record is not null)id=world.JoinCharacter(record);
                    }
                } catch(Exception e) when(e is JsonException or OperationCanceledException or WebSocketException){return;}
            }
            if(id is null){using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(1));await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation,"Authentication required or character already connected",deadline.Token);return;}
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            await Send(socket, new { type = "welcome", version = 9, id, tickRate = 20 }, lifetime.Token);
            var receive = Receive(socket, world, id, lifetime.Token);
            var send = Snapshots(socket, world, id, lifetime.Token);
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
        finally { if(id is not null) world.Leave(id); }
    }

    private static async Task Send(WebSocket socket, object value, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        await socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)).AsMemory(), WebSocketMessageType.Text, true, timeout.Token);
    }

    private static async Task Snapshots(WebSocket socket, World world, string id, CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        while (await timer.WaitForNextTickAsync(token)) await Send(socket, world.View(id), token);
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
                !root.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("seq", out var seq) || seq.ValueKind != JsonValueKind.Number || !seq.TryGetInt64(out var sequence) || sequence < 0)
                throw new InvalidDataException("Invalid envelope");
            if (type.GetString() == "input")
            {
                if (!root.TryGetProperty("x", out var x) || x.ValueKind != JsonValueKind.Number || !x.TryGetDouble(out var dx) || !double.IsFinite(dx) || Math.Abs(dx) > 1000 ||
                    !root.TryGetProperty("z", out var z) || z.ValueKind != JsonValueKind.Number || !z.TryGetDouble(out var dz) || !double.IsFinite(dz) || Math.Abs(dz) > 1000)
                    throw new InvalidDataException("Invalid direction");
                world.Input(id, sequence, dx, dz);
            }
            else if (type.GetString() == "action")
            {
                if (!root.TryGetProperty("kind", out var kind) || kind.ValueKind != JsonValueKind.String ||
                    kind.GetString() is not ("target" or "attack" or "interact" or "loot" or "stat" or "farm_stat" or "buy" or "sell" or "equip" or "craft" or "gather" or "claim" or "use" or "pet" or "skill" or "teleport" or "slot" or "party_invite" or "party_accept" or "party_leave" or "party_kick" or "party_promote" or "chat_local" or "chat_party" or "chat_private" or "trade_open" or "trade_offer" or "trade_confirm" or "trade_cancel" or "pet_hatch" or "pet_breed" or "pet_evolve" or "pet_train" or "pet_select" or "pet_take" or "farm_start" or "farm_stop" or "home_intro" or "home_claim" or "home_rent" or "home_visit" or "home_stairs" or "home_floor" or "home_expand" or "home_place" or "home_move" or "home_remove" or "home_put" or "home_gear_put" or "home_gear_take" or "home_take" or "home_craft" or "home_produce" or "home_cancel" or "home_collect" or "home_breed" or "home_plant" or "home_harvest" or "guild_create" or "guild_accept" or "guild_invite" or "guild_kick" or "guild_rank" or "guild_leave" or "guild_quest" or "guild_buff" or "guild_hall" or "guild_put" or "guild_gear_put" or "guild_gear_take" or "guild_take" or "market_search" or "market_list" or "market_pet" or "market_buy" or "market_cancel" or "transit" or "world_transit" or "forest_transit" or "market_claim" or "market_page" or "stall_open" or "stall_close" or "stall_list" or "stall_buy" or "stall_claim" or "guild_release" or "chat_guild" or "reset_stats" or "prestige" or "save_now" or "appearance" or "gear_unpack" or "gear_equip" or "gear_remove" or "gear_legacy_remove" or "gear_repair" or "arena_invite" or "arena_accept" or "arena_hit" or "arena_skill" or "arena_leave" or "arena_ready" or "arena_team_invite" or "arena_war_invite"))
                    throw new InvalidDataException("Invalid action");
                var target = "";
                if (root.TryGetProperty("target", out var value))
                {
                    if (value.ValueKind != JsonValueKind.String || value.GetString()!.Length > 64) throw new InvalidDataException("Invalid target");
                    target = value.GetString()!;
                }
                var text="";
                if(root.TryGetProperty("text",out var content)){
                    if(content.ValueKind!=JsonValueKind.String||content.GetString()!.Length>256)throw new InvalidDataException("Invalid text");
                    text=content.GetString()!;
                }
                world.Act(id, sequence, kind.GetString()!, target,text);
            }
            else throw new InvalidDataException("Unknown type");
        }
    }
}
