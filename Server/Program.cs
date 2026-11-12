using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseWebSockets();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "MMORPG-2030", multiplayerReady = false }));
app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure,
        "Multiplayer protocol not implemented", CancellationToken.None);
});
app.Run();
