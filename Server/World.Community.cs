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
        if(!(kind.StartsWith("home_")||kind.StartsWith("guild_")||kind.StartsWith("market_")))return false;
        if(p.Data.AccountId==""){p.Message="Diese persistenten Systeme benötigen einen eigenen Online-Charakter.";return true;}
        if(kind=="market_search"){p.MarketSearch=text.Trim()[..Math.Min(40,text.Trim().Length)];return true;}
        try{
            var w=SharedWorld.Copy(Shared);var c=CharacterRules.Clone(Export(p));
            var message=kind.StartsWith("home_")?HousingAction(w,c,kind,target,text):kind.StartsWith("guild_")?GuildAction(w,c,kind,target,text):MarketAction(w,c,kind,target,text);
            Store.SaveWorld(w,[c]);sharedState=w;Import(p,c);p.Message=message;
            // Revocation/rent changes must eject an already-present unauthorized visitor as well.
            foreach(var visitor in players.Values)if(!HomeAccess(visitor,visitor.X,visitor.Z)){visitor.X=district.Spawn.X;visitor.Z=district.Spawn.Z;visitor.Dx=visitor.Dz=0;visitor.Message="Besuchsrecht fehlt oder wurde widerrufen.";}
        }catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){p.Message=e is GameRuleException?e.Message:"Datenbankfehler: keine Übertragung gespeichert.";}
        return true;
    }
    private static void Need(bool ok,string message){if(!ok)throw new GameRuleException(message);}
    private static string DropItem(Drop drop)=>drop.MonsterId=="worldboss-1"?"boss_core":"scrap";
    private static void Take(CharacterState c,string item,int count){Need(count>0&&c.Inventory.GetValueOrDefault(item)>=count,"Material fehlt.");c.Inventory[item]-=count;if(c.Inventory[item]==0)c.Inventory.Remove(item);}
    private void Give(CharacterState c,string item,int count){Need(CharacterRules.Add(c,item,count,Catalog),"Inventar voll.");}
    private static int Number(string value,int min,int max){Need(int.TryParse(value,out var n)&&n>=min&&n<=max,"Ungültige Anzahl.");return n;}
    private static void Coins(CharacterState c,int amount){Need((long)c.Coins+amount is >=0 and <=int.MaxValue,"Münzen fehlen oder Limit erreicht.");c.Coins+=amount;}
    private bool CanWalk(Player p,double x,double z)=>district.Walkable(x,z)&&HomeAccess(p,x,z)&&!Shared.Homes.Values.SelectMany(h=>h.Furniture.Values).Any(f=>Distance(x,z,f.X,f.Z)<.65);
    private bool HomeAccess(Player p,double x,double z)
    {
        foreach(var def in district.Homes.Where(d=>Math.Abs(x-d.X)<8.8&&Math.Abs(z-d.Z)<5.8)){
            if(def.GuildHall){var g=GuildOf(Shared,p.Id);return g?.Hall==def.Id;}
            if(!Shared.Homes.TryGetValue(def.Id,out var h))return false;
            var allowed=h.Owner==p.Id||(h.RentUntil>Utc&&(h.Visitors.Contains(p.Id)||(h.GuildVisitors&&GuildOf(Shared,h.Owner) is {} ownerGuild&&ownerGuild.Members.ContainsKey(p.Id))));
            if(!allowed||h.Rooms==1&&x>def.X-.5)return false;
        }
        return true;
    }
    private object CommunityView(Player p)
    {
        var w=Shared;var guild=GuildOf(w,p.Id);var search=p.MarketSearch;
        return new{
            housingIntro=p.Data.State.HousingIntro,
            homes=district.Homes.Select(d=>{
                w.Homes.TryGetValue(d.Id,out var h);var mine=h?.Owner==p.Id;
                return new{id=d.Id,name=d.Name,x=d.X,z=d.Z,guildHall=d.GuildHall,owner=h?.Owner??"",rooms=h?.Rooms??1,rentUntil=h?.RentUntil??0,
                    furniture=h?.Furniture.Select(f=>new{id=f.Key,item=f.Value.Item,x=f.Value.X,z=f.Value.Z,yaw=f.Value.Yaw}).ToArray(),
                    storage=mine?h?.Storage:null,visitors=mine?h?.Visitors:null,guildVisitors=mine&&h!.GuildVisitors,harvestAt=mine?h?.HarvestAt:0};}).ToArray(),
            guild=guild is null?null:new{name=guild.Name,leader=guild.Leader,members=guild.Members,level=1+guild.Exp/100,exp=guild.Exp,quest=guild.Quest,influence=guild.Influence,storage=guild.Storage,buffUntil=guild.BuffUntil,hall=guild.Hall},
            guildInvites=w.Guilds.Where(g=>g.Value.Invitations.GetValueOrDefault(p.Id)>Utc).Select(g=>new{id=g.Key,name=g.Value.Name}).ToArray(),
            auctions=w.Auctions.Where(a=>search.Length==0||Catalog.Items[a.Value.Item].Name.Contains(search,StringComparison.OrdinalIgnoreCase)).OrderByDescending(a=>a.Value.Created).Take(40).Select(a=>new{id=a.Key,seller=a.Value.Seller,item=a.Value.Item,count=a.Value.Count,price=a.Value.Price}).ToArray(),
            ownAuctions=w.Auctions.Where(a=>a.Value.Seller==p.Id).Select(a=>new{id=a.Key,item=a.Value.Item,count=a.Value.Count,price=a.Value.Price}).ToArray(),
            proceeds=w.Proceeds.GetValueOrDefault(p.Id),marketSearch=search,serverUtc=Utc
        };
    }
}
