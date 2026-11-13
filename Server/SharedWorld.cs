using System.Text.Json;
namespace MMORPG.Server;

// Small-world aggregate: one CAS revision and one SQL transaction with character changes.
// This is intentionally single-world-server persistence, not a multi-shard architecture.
public sealed class SharedWorld
{
    public Dictionary<string,StallState> Stalls {get;set;}=[];
    public long Version {get;set;}
    public Dictionary<string,HomeState> Homes {get;set;}=[];
    public Dictionary<string,GuildState> Guilds {get;set;}=[];
    public Dictionary<string,AuctionState> Auctions {get;set;}=[];
    public Dictionary<string,int> Proceeds {get;set;}=[];
    public Dictionary<string,long> ArenaRewards {get;set;}=[];
    public static T Copy<T>(T value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
}
public sealed class ProductionJob {public string Recipe {get;set;}="";public string Item {get;set;}="";public string Profession {get;set;}="";public int Count {get;set;}public long ReadyAt {get;set;}}
public sealed class HomeState
{
    public ProductionJob? Production {get;set;}
    public string Owner {get;set;}="";
    public HashSet<string> Visitors {get;set;}=[];
    public bool GuildVisitors {get;set;}
    public int Rooms {get;set;}=1;
    public long RentUntil {get;set;}
    public Dictionary<string,FurnitureState> Furniture {get;set;}=[];
    public Dictionary<string,GearState> GearStorage {get;set;}=[];
    public Dictionary<string,int> Storage {get;set;}=[];
    public long HarvestAt {get;set;}
}
public sealed class FurnitureState
{
    public string Item {get;set;}="oak_chair";public double X {get;set;}public double Z {get;set;}public double Yaw {get;set;}
}
public sealed class GuildState
{
    public string Name {get;set;}="";public string Leader {get;set;}="";
    public Dictionary<string,string> Members {get;set;}=[];
    public Dictionary<string,long> Invitations {get;set;}=[];
    public Dictionary<string,GearState> GearStorage {get;set;}=[];
    public Dictionary<string,int> Storage {get;set;}=[];
    public int Exp {get;set;}public int Quest {get;set;}public int Influence {get;set;}
    public long BuffUntil {get;set;}
    public string Hall {get;set;}="";
}
public sealed class StallState : Point {public string Title {get;set;}="";public long Until {get;set;}}
public sealed class AuctionState
{
    public string StallId {get;set;}="";
    public PetState? Pet {get;set;}
    public GearState? Gear {get;set;}public string GearId {get;set;}="";
    public string Seller {get;set;}="";public string Item {get;set;}="";
    public int Count {get;set;}public int Price {get;set;}public long Created {get;set;}
}
