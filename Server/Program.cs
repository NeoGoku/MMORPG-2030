using MMORPG.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<World>();
builder.Services.AddHostedService(services => services.GetRequiredService<World>());
var app = builder.Build();
app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok", service = "MMORPG-2030", protocolVersion = 1,
    multiplayerReady = true, persistenceReady = false
}));
app.Map("/ws", (HttpContext context, World world) => Session.Run(context, world));
app.Run();
