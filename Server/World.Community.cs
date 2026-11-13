using System.Text.Json;
namespace MMORPG.Server;
public sealed partial class World
{
    private SharedWorld? sharedState;
    private SharedWorld Shared=>sharedState??=Store.LoadWorld();
    private static long Utc=>DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private GuildState? GuildOf(SharedWorld w,string id)=>w.Guilds.Values.FirstOrDefault(g=>g.Members.ContainsKey(id));
    private bool CommunityAction(Player p,string kind,string target,string text)
    {
        if(!(kind.StartsWith("home_")||kind.StartsWith("guild_")||kind.StartsWith("market_")||kind.StartsWith("stall_")))return false;
        if(p.Data.AccountId==""){p.Message="Diese persistenten Systeme benötigen einen eigenen Online-Charakter.";return true;}
        if(kind=="market_page"){if(int.TryParse(target,out var page))p.MarketPage=Math.Clamp(page,0,199);return true;}
        if(kind=="market_search"){p.MarketPage=0;p.MarketSearch=text.Trim()[..Math.Min(40,text.Trim().Length)];return true;}
        try{
            var w=SharedWorld.Copy(Shared);var c=CharacterRules.Clone(Export(p));
            var message=kind.StartsWith("home_")?HousingAction(w,c,kind,target,text):kind.StartsWith("guild_")?GuildAction(w,c,kind,target,text):kind.StartsWith("stall_")?StallAction(w,c,kind,target,text):MarketAction(w,c,kind,target,text);
            Store.SaveWorld(w,[c]);sharedState=w;Import(p,c);p.SavedTick=tick;p.Message=message;if(kind=="home_stairs"){StopFarm(p);p.Target="";}
            // Revocation/rent changes must eject an already-present unauthorized visitor as well.
            foreach(var visitor in players.Values)if(!HomeAccess(visitor,visitor.X,visitor.Z)){ResetInterior(visitor);visitor.X=district.Spawn.X;visitor.Z=district.Spawn.Z;visitor.Dx=visitor.Dz=0;visitor.Message="Besuchsrecht fehlt oder wurde widerrufen.";}
        }catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){p.Message=e is GameRuleException?e.Message:"Datenbankfehler: keine Übertragung gespeichert.";}
        return true;
    }
    private static void Need(bool ok,string message){if(!ok)throw new GameRuleException(message);}
    private string DropItem(Drop drop)=>monsters.TryGetValue(drop.MonsterId,out var enemy)&&enemy.Spawn.LootItem!=""?enemy.Spawn.LootItem:drop.MonsterId=="worldboss-1"?"boss_core":drop.MonsterId=="worldboss-forest"?"root_core":"scrap";
    private static void Take(CharacterState c,string item,int count){Need(count>0&&c.Inventory.GetValueOrDefault(item)>=count,"Material fehlt.");c.Inventory[item]-=count;if(c.Inventory[item]==0)c.Inventory.Remove(item);}
    private void Give(CharacterState c,string item,int count){Need(CharacterRules.Add(c,item,count,Catalog),"Inventar voll.");}
    private static int Number(string value,int min,int max){Need(int.TryParse(value,out var n)&&n>=min&&n<=max,"Ungültige Anzahl.");return n;}
    private static void Coins(CharacterState c,int amount){Need((long)c.Coins+amount is >=0 and <=int.MaxValue,"Münzen fehlen oder Limit erreicht.");c.Coins+=amount;}
    private bool RegionAccess(Player p,double x,double z)=>district.Regions.All(r=>Distance(x,z,r.X,r.Z)>=r.Radius||p.Level>=r.MinLevel);
    private string RegionName(Player p)=>district.Regions.FirstOrDefault(r=>Distance(p.X,p.Z,r.X,r.Z)<r.Radius)?.Name??"Nordtor";
    private bool CanWalk(Player p,double x,double z)=>RegionAccess(p,x,z)&&district.Walkable(x,z)&&HomeAccess(p,x,z)&&!Shared.Homes.Values.SelectMany(h=>h.Furniture.Values).Any(f=>f.Floor==p.Data.State.Floor&&Distance(x,z,f.X,f.Z)<Catalog.Items[f.Item].FurnitureRadius+.15);
    private bool MayVisit(SharedWorld w,HomeState h,string id)=>h.Owner==id||(h.RentUntil>Utc&&(h.Visitors.Contains(id)||(h.GuildVisitors&&GuildOf(w,h.Owner) is {} guild&&guild.Members.ContainsKey(id))));
    private bool HomeAccess(Player p,double x,double z)
    {
        var state=p.Data.State;
        if(state.Floor!=0){
            var upper=district.Homes.FirstOrDefault(d=>d.Id==state.HomeId&&!d.GuildHall);
            return state.Floor>0&&upper is not null&&Shared.Homes.TryGetValue(upper.Id,out var home)&&state.Floor<home.Floors&&MayVisit(Shared,home,p.Id)&&Math.Abs(x-upper.X)<8.8&&Math.Abs(z-upper.Z)<5.8&&(home.Rooms>1||x<upper.X-.5);
        }
        foreach(var def in district.Homes.Where(d=>Math.Abs(x-d.X)<8.8&&Math.Abs(z-d.Z)<5.8)){
            if(def.GuildHall){var g=GuildOf(Shared,p.Id);return g?.Hall==def.Id;}
            if(!Shared.Homes.TryGetValue(def.Id,out var h))return false;
            var allowed=MayVisit(Shared,h,p.Id);
            if(!allowed||h.Rooms==1&&x>def.X-.5)return false;
        }
        return true;
    }
    private object CommunityView(Player p)
    {
        var w=Shared;var guild=GuildOf(w,p.Id);var search=p.MarketSearch;var offers=w.Auctions.Where(a=>a.Value.StallId==""&&a.Value.Created+Catalog.AuctionDays*86400L>Utc&&(search.Length==0||Catalog.Items[a.Value.Item].Name.Contains(search,StringComparison.OrdinalIgnoreCase))).OrderByDescending(a=>a.Value.Created).ThenBy(a=>a.Key).ToArray();var pageSize=Math.Clamp(Catalog.MarketPageSize,1,40);var pages=Math.Max(1,(offers.Length+pageSize-1)/pageSize);p.MarketPage=Math.Min(p.MarketPage,pages-1);
        return new{
            ownStall=w.Stalls.TryGetValue(p.Id,out var ownStall)?ownStall:null,
            stalls=w.Stalls.Where(entry=>entry.Value.Until>Utc&&Distance(p.X,p.Z,entry.Value.X,entry.Value.Z)<=Catalog.InterestRadius).Select(entry=>new{id=entry.Key,title=entry.Value.Title,x=entry.Value.X,z=entry.Value.Z,until=entry.Value.Until,offers=w.Auctions.Where(a=>a.Value.StallId==entry.Key&&a.Value.Created+Catalog.AuctionDays*86400L>Utc).Select(a=>new{id=a.Key,item=a.Value.Item,count=a.Value.Count,price=a.Value.Price,condition=a.Value.Gear?.Condition}).ToArray()}).ToArray(),
            housingIntro=p.Data.State.HousingIntro,
            homes=district.Homes.Select(d=>{
                w.Homes.TryGetValue(d.Id,out var h);var mine=h?.Owner==p.Id;
                return new{id=d.Id,name=d.Name,x=d.X,z=d.Z,guildHall=d.GuildHall,owner=h?.Owner??"",floors=h?.Floors??1,floorHeight=Catalog.HousingFloorHeight,rooms=h?.Rooms??1,rentUntil=h?.RentUntil??0,
                    furniture=h?.Furniture.Select(f=>new{id=f.Key,floor=f.Value.Floor,item=f.Value.Item,x=f.Value.X,z=f.Value.Z,yaw=f.Value.Yaw}).ToArray(),
                    gearStorage=mine?h?.GearStorage:null,production=mine?h?.Production:null,storage=mine?h?.Storage:null,visitors=mine?h?.Visitors:null,guildVisitors=mine&&h!.GuildVisitors,harvestAt=mine?h?.HarvestAt:0};}).ToArray(),
            guild=guild is null?null:new{name=guild.Name,leader=guild.Leader,members=guild.Members,level=1+guild.Exp/100,exp=guild.Exp,quest=guild.Quest,influence=guild.Influence,gearStorage=guild.GearStorage,storage=guild.Storage,buffUntil=guild.BuffUntil,hall=guild.Hall},
            guildInvites=w.Guilds.Where(g=>g.Value.Invitations.GetValueOrDefault(p.Id)>Utc).Select(g=>new{id=g.Key,name=g.Value.Name}).ToArray(),
            marketPage=p.MarketPage,marketPages=pages,
            auctions=offers.Skip(p.MarketPage*pageSize).Take(pageSize).Select(a=>new{id=a.Key,seller=a.Value.Seller,item=a.Value.Item,petLevel=a.Value.Pet?.Level??0,condition=a.Value.Gear?.Condition,count=a.Value.Count,price=a.Value.Price}).ToArray(),
            ownAuctions=w.Auctions.Where(a=>a.Value.Seller==p.Id).Select(a=>new{id=a.Key,item=a.Value.Item,petLevel=a.Value.Pet?.Level??0,condition=a.Value.Gear?.Condition,count=a.Value.Count,price=a.Value.Price,expired=a.Value.Created+Catalog.AuctionDays*86400L<=Utc}).ToArray(),
            proceeds=w.Proceeds.GetValueOrDefault(p.Id),marketSearch=search,serverUtc=Utc
        };
    }
}
