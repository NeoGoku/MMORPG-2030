using System.Diagnostics;

namespace MMORPG.Server;

// One shared, ephemeral world. Only the server advances positions.
public sealed class World : BackgroundService
{
    private readonly object gate = new();
    private readonly Dictionary<string, Player> players = new();
    private long tick;
    private sealed class Player(string id)
    {
        public string Id { get; } = id;
        public double X, Z, InputX, InputZ;
        public long Sequence = -1;
        public long LastInput;
    }

    public string? Join()
    {
        lock (gate)
        {
            if (players.Count >= 100) return null;
            var id = Guid.NewGuid().ToString("N");
            players.Add(id, new Player(id));
            return id;
        }
    }

    public void Leave(string id) { lock (gate) players.Remove(id); }

    public void Input(string id, long sequence, double x, double z)
    {
        lock (gate)
        {
            if (!players.TryGetValue(id, out var p) || sequence <= p.Sequence) return;
            var length = Math.Sqrt(x * x + z * z);
            p.InputX = x / Math.Max(1, length);
            p.InputZ = z / Math.Max(1, length);
            p.Sequence = sequence;
            p.LastInput = Stopwatch.GetTimestamp();
        }
    }

    public object Snapshot()
    {
        lock (gate) return new
        {
            type = "snapshot", tick,
            players = players.Values.Select(p => new { id = p.Id, x = p.X, z = p.Z }).ToArray()
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            lock (gate)
            {
                tick++;
                foreach (var p in players.Values)
                {
                    if (Stopwatch.GetElapsedTime(p.LastInput).TotalSeconds > .25) continue;
                    p.X = Math.Clamp(p.X + p.InputX * 5 * .05, -19, 19);
                    p.Z = Math.Clamp(p.Z + p.InputZ * 5 * .05, -19, 19);
                }
            }
        }
    }
}
