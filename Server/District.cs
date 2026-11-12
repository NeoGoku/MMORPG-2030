using System.Text.Json;

namespace MMORPG.Server;

public class Point { public double X { get; set; } public double Z { get; set; } }
public sealed class Building
{
    public double X { get; set; } public double Z { get; set; }
    public double Width { get; set; } public double Depth { get; set; }
}
public sealed class Spawn
{
    public string Id { get; set; } = ""; public string Name { get; set; } = "";
    public string DamageType {get;set;}="physical";public double Reach {get;set;}=2;public double Speed {get;set;}=1.8;
    public double X { get; set; } public double Z { get; set; }
}
public sealed class Rules
{
    public double Speed { get; set; } public int PlayerHp { get; set; } public int MonsterHp { get; set; }
    public int PlayerDamage { get; set; } public int MonsterDamage { get; set; }
    public double AttackSeconds { get; set; } public double MonsterAttackSeconds { get; set; }
    public double AttackRange { get; set; } public double AggroRange { get; set; } public double MonsterSpeed { get; set; }
    public double MonsterRespawnSeconds { get; set; } public double PlayerRespawnSeconds { get; set; }
    public int KillExp { get; set; } public int QuestKills { get; set; } public int QuestExp { get; set; }
    public int QuestCoins { get; set; } public int LootCoins { get; set; } public int LootScrap { get; set; }
}
public sealed class District
{
    public double Bounds { get; set; }
    public Point Spawn { get; set; } = new(); public Spawn Npc { get; set; } = new();
    public Building[] Buildings { get; set; } = []; public Spawn[] Monsters { get; set; } = [];
    public Rules Rules { get; set; } = new();
    public static District Load(string path) => JsonSerializer.Deserialize<District>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("District missing");
    public bool Walkable(double x, double z, double radius = .45) => Math.Abs(x) <= Bounds-radius && Math.Abs(z) <= Bounds-radius && !Buildings.Any(b=>Math.Abs(x-b.X)<b.Width/2+radius&&Math.Abs(z-b.Z)<b.Depth/2+radius);
}
