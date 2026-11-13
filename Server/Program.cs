using MMORPG.Server;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder=WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=16384);
var catalog=GameCatalog.Load(Path.Combine(AppContext.BaseDirectory,"game.json"));
builder.Services.AddSingleton(catalog);
builder.Services.AddSingleton<IGameStore>(_=>string.IsNullOrWhiteSpace(builder.Configuration["MMORPG_DATABASE"])?new MemoryGameStore():new PostgresGameStore(builder.Configuration["MMORPG_DATABASE"]!));
builder.Services.AddSingleton<AccountService>();
builder.Services.AddSingleton(District.Load(Path.Combine(AppContext.BaseDirectory,"district.json")));
builder.Services.AddSingleton<World>();builder.Services.AddHostedService(s=>s.GetRequiredService<World>());
builder.Services.AddRateLimiter(options=>{options.RejectionStatusCode=429;options.AddPolicy("auth",context=>RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString()??"local",_=>new FixedWindowRateLimiterOptions{PermitLimit=20,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));});
builder.Services.AddCors(o=>o.AddDefaultPolicy(p=>p.WithOrigins((builder.Configuration["MMORPG_WEB_ORIGINS"]??"http://127.0.0.1:8080,http://localhost:8080").Split(',',StringSplitOptions.RemoveEmptyEntries)).AllowAnyHeader().WithMethods("GET","POST")));
var app=builder.Build();
app.Use(async(context,next)=>{try{await next();}catch(GameRuleException e){context.Response.StatusCode=400;await context.Response.WriteAsJsonAsync(new{error=e.Message});}});
app.UseCors();app.UseRateLimiter();app.UseWebSockets(new WebSocketOptions{KeepAliveInterval=TimeSpan.FromSeconds(20)});
app.MapGet("/health",(IGameStore store)=>Results.Ok(new{status="ok",service="MMORPG-2030",protocolVersion=9,multiplayerReady=true,persistenceReady=store.Durable,guestMode=builder.Configuration["MMORPG_ALLOW_GUEST"]=="true"}));
app.MapPost("/api/register",(Credentials request,AccountService auth)=>Results.Ok(auth.Register(request.Username,request.Password))).RequireRateLimiting("auth");
app.MapPost("/api/login",(Credentials request,AccountService auth)=>auth.Login(request.Username,request.Password) is {} s?Results.Ok(s):Results.Unauthorized()).RequireRateLimiting("auth");
string? Account(HttpContext c,AccountService auth){var header=c.Request.Headers.Authorization.ToString();return header.StartsWith("Bearer ")?auth.Resolve(header[7..]):null;}
app.MapGet("/api/characters",(HttpContext c,AccountService auth)=>Account(c,auth) is {} account?Results.Ok(auth.List(account)):Results.Unauthorized());
app.MapPost("/api/characters",(CreateCharacter request,HttpContext c,AccountService auth)=>Account(c,auth) is {} account?Results.Ok(auth.CreateCharacter(account,request.Slot,request.Name,request.ClassId,request.Appearance)):Results.Unauthorized());
app.MapPost("/api/ticket",(TicketRequest request,HttpContext c,AccountService auth)=>Account(c,auth) is not null?Results.Ok(new{ticket=auth.Ticket(c.Request.Headers.Authorization.ToString()[7..],request.CharacterId)}):Results.Unauthorized());
app.Map("/ws",(HttpContext c,World world,AccountService auth)=>Session.Run(c,world,auth,builder.Configuration["MMORPG_ALLOW_GUEST"]=="true"));
app.Run();
public sealed record Credentials(string Username,string Password);
public sealed record CreateCharacter(int Slot,string Name,string ClassId,string Appearance="default");
public sealed record TicketRequest(string CharacterId);
