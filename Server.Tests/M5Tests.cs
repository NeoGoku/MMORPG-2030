using MMORPG.Server;
using System.Text.Json;
public static class M5Tests
{
    static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    public static void Run()
    {
        void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS M5: "+label);}
        var catalog=GameCatalog.Load("Client/data/game.json");var district=District.Load("Client/data/district.json");
        IGameStore store=Environment.GetEnvironmentVariable("MMORPG_TEST_DATABASE") is {} db?new PostgresGameStore(db):new MemoryGameStore();
        CharacterRecord Hero(string name,int coins=100){var account=Guid.NewGuid().ToString("N");store.Register(new(account,"m5_"+account,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=account,Name=name,State=new CharacterState{X=30,Z=24,Coins=coins,Inventory=new(){{"wood",70},{"ore",30},{"oak_chair",2},{"herb",4}}}};store.Create(c);return c;}
        var ca=Hero("Besitzer");var cb=Hero("Gast");var world=new World(district,catalog,store);var a=world.JoinCharacter(ca)!;var b=world.JoinCharacter(cb)!;long sa=0,sb=0;
        void Act(string id,string kind,string target="",string text=""){if(id==a)world.Act(id,++sa,kind,target,text);else world.Act(id,++sb,kind,target,text);}
        JsonElement View(string id)=>JsonSerializer.SerializeToElement(world.View(id),Json);
        Act(a,"home_intro");Act(a,"home_claim","home-1");
        Check(View(a).TryGetProperty("community",out var community)&&community.GetProperty("homes").EnumerateArray().Any(h=>h.GetProperty("owner").GetString()==a),"housing quest grants a real persistent apartment");
        var home=community.GetProperty("homes").EnumerateArray().First(h=>h.GetProperty("owner").GetString()==a);var hid=home.GetProperty("id").GetString()!;var hx=home.GetProperty("x").GetDouble();var hz=home.GetProperty("z").GetDouble();
        void At(string id,double x,double z){world.Leave(id);var c=store.Get(id)!;c.State.X=x;c.State.Z=z;store.Save(c);world.JoinCharacter(c);if(id==a)sa=0;else sb=0;}
        int Item(string id,string item)=>store.Get(id)!.State.Inventory.GetValueOrDefault(item);
        At(a,hx-4,hz);Act(a,"home_place",hid,$"oak_chair|{hx-6}|{hz-2}|1.5");
        var furniture=View(a).GetProperty("community").GetProperty("homes").EnumerateArray().First(h=>h.GetProperty("id").GetString()==hid).GetProperty("furniture");
        Check(furniture.GetArrayLength()==1&&Item(a,"oak_chair")==1,"placed furniture consumes inventory once");var fid=furniture[0].GetProperty("id").GetString()!;
        Act(b,"home_remove",hid,fid);Check(Item(b,"oak_chair")==2,"visitor cannot steal furniture");
        Act(a,"home_move",hid,$"{fid}|{hx-6}|{hz-1}|2.5");Act(a,"home_remove",hid,fid);Act(a,"home_remove",hid,fid);Check(Item(a,"oak_chair")==2,"move/remove replay conserves furniture");
        Act(a,"home_put",hid,"herb|2");Act(a,"home_take",hid,"herb|1");Check(Item(a,"herb")==3,"housing storage conserves inventory");
        Act(a,"home_visit",hid,b+"|allow");At(b,hx-4,hz);Check(View(b).GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("id").GetString()==b).GetProperty("x").GetDouble()==hx-4,"authorized visitor enters physical apartment");
        Act(a,"home_visit",hid,b+"|remove");Check(View(b).GetProperty("players").EnumerateArray().Single(p=>p.GetProperty("id").GetString()==b).GetProperty("z").GetDouble()==district.Spawn.Z,"revocation ejects an already present visitor");
        Act(a,"home_expand",hid);Act(a,"home_plant",hid);Act(a,"home_harvest",hid);Check(Item(a,"herb")==2,"garden consumes seed and rejects premature harvest");
        At(a,30,24);At(b,30,24);Act(a,"guild_create","","Sternenbund "+Guid.NewGuid().ToString("N")[..5]);Act(a,"guild_invite",b);
        var invite=View(b).GetProperty("community").GetProperty("guildInvites")[0].GetProperty("id").GetString()!;Act(b,"guild_accept",invite);
        Act(a,"guild_put","","wood|20");Act(a,"guild_put","","ore|10");Act(b,"guild_take","","wood|1");Check(Item(b,"wood")==70,"ordinary member cannot withdraw guild treasury");
        Act(a,"guild_rank",b,"officer");Act(b,"guild_take","","wood|1");Check(Item(b,"wood")==71,"officer rank enables authorized withdrawal");Act(b,"guild_put","","wood|1");Act(a,"guild_hall","guildhall-1");Act(a,"guild_quest");Act(a,"guild_buff");
        Check(View(a).GetProperty("community").GetProperty("guild").GetProperty("buffUntil").GetInt64()>0,"guild material quest funds actual shared timed buff");
        Check(View(a).GetProperty("community").GetProperty("guild").GetProperty("exp").GetInt32()==25,"guild blessing consumes energy without decreasing earned guild EXP");
        At(a,30,12);At(b,30,12);Act(a,"market_list","","oak_chair|1|20");var listing=View(b).GetProperty("community").GetProperty("auctions")[0].GetProperty("id").GetString()!;var before=store.Get(b)!.State.Coins;
        world.Leave(a);Act(b,"market_buy",listing);Act(b,"market_buy",listing);Check(Item(b,"oak_chair")==3&&store.Get(b)!.State.Coins==before-20,"offline seller escrow purchase is single-use");
        world.JoinCharacter(store.Get(a)!);sa=0;Act(a,"market_claim");var paid=store.Get(a)!.State.Coins;Act(a,"market_claim");Check(store.Get(a)!.State.Coins==paid,"seller proceeds claim cannot repeat");
        var stale=store.LoadWorld();var fresh=store.LoadWorld();store.SaveWorld(fresh,[]);var character=store.Get(b)!;var oldCoins=character.State.Coins;character.State.Coins++;
        try{store.SaveWorld(stale,[character]);throw new Exception("stale world accepted");}catch(GameRuleException){}Check(store.Get(b)!.State.Coins==oldCoins,"stale world transaction cannot mutate character");
        var candidate=store.LoadWorld();var oldCharacter=store.Get(b)!;var newer=store.Get(b)!;newer.State.Coins++;store.Save(newer);candidate.Proceeds[a]=999;
        try{store.SaveWorld(candidate,[oldCharacter]);throw new Exception("stale character accepted");}catch(GameRuleException){}Check(store.LoadWorld().Proceeds.GetValueOrDefault(a)!=999,"stale character rolls back world write");
        world.Leave(a);world.Leave(b);
        var reopened=new World(district,catalog,store);var joined=reopened.JoinCharacter(store.Get(a)!)!;var restored=JsonSerializer.SerializeToElement(reopened.View(joined),Json).GetProperty("community");
        Check(restored.GetProperty("homes").EnumerateArray().Any(h=>h.GetProperty("owner").GetString()==a&&h.GetProperty("rooms").GetInt32()==2)&&restored.GetProperty("guild").GetProperty("hall").GetString()=="guildhall-1","new world restores housing and guild house membership");
        reopened.Leave(joined);(store as IDisposable)?.Dispose();
    }
}
