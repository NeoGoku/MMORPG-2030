using MMORPG.Server;
public static class WorkshopTests
{
    public static void Run()
    {
        var g=GameCatalog.Load("Client/data/game.json");var d=District.Load("Client/data/district.json");
        var memory=new MemoryGameStore();using var postgres=Environment.GetEnvironmentVariable("MMORPG_TEST_DATABASE") is {} cs?new PostgresGameStore(cs):null;IGameStore store=postgres is null?memory:postgres;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS workshops: "+text);}
        var account=Guid.NewGuid().ToString("N");store.Register(new(account,account,new byte[32],new byte[32]));
        var c=new CharacterRecord{AccountId=account,State=new(){X=58,Z=34,Floor=1,HomeId="home-2",Inventory=new(){{"ore",6},{"wood",6},{"herb",6}}}};PetRules.Normalize(c);store.Create(c);
        var shared=store.LoadWorld();var previous=shared.Homes.GetValueOrDefault("home-2");
        shared.Homes["home-2"]=new(){Owner=c.Id,Rooms=2,Floors=2,RentUntil=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+3600,Furniture=new(){{"forge",new(){Item="forge_station",Floor=1,X=58,Z=32}},{"loom",new(){Item="loom_station",Floor=1,X=58,Z=36}}}};store.SaveWorld(shared,[]);
        var world=new World(d,g,store);world.JoinCharacter(c);long seq=0;
        void Act(string kind,string text)=>world.Act(c.Id,++seq,kind,"home-2",text);
        Act("home_work_start","loom|iron_sword");Check(store.LoadWorld().Homes["home-2"].WorkOrders.Count==0,"station rejects incompatible recipe without taking materials");
        Act("home_work_start","forge|iron_sword");Act("home_work_start","loom|woven_coat");Act("home_work_start","forge|iron_sword");Act("home_remove","forge");
        Check(store.LoadWorld().Homes["home-2"].WorkOrders.Count==2&&store.LoadWorld().Homes["home-2"].Furniture.ContainsKey("forge")&&store.Get(c.Id)!.State.Inventory["ore"]==3,"parallel station escrow rejects duplicate start and busy removal");world.Leave(c.Id);
        shared=store.LoadWorld();foreach(var job in shared.Homes["home-2"].WorkOrders.Values)job.ReadyAt=1;store.SaveWorld(shared,[]);
        world=new World(d,g,store);world.JoinCharacter(store.Get(c.Id)!);seq=0;g.Recipes["woven_coat"].Inputs["wood"]=99;
        Act("home_work_collect","forge");Act("home_work_collect","forge");Act("home_work_cancel","loom");Act("home_work_cancel","loom");
        var saved=store.Get(c.Id)!;
        Check(saved.State.Inventory.GetValueOrDefault("iron_sword")==1&&saved.State.Inventory["wood"]==6&&saved.State.Inventory["herb"]==6&&saved.State.Professions["smithing"]==g.ProfessionCraftXp&&saved.State.FarmXp=="25","restart collection/progression and original-material cancellation occur once");
        Act("home_work_start","forge|iron_sword");world.Leave(c.Id);saved=store.Get(c.Id)!;saved.State.Inventory=new(){{"potion",g.InventorySlots*g.Items["potion"].Stack}};store.Save(saved);
        shared=store.LoadWorld();shared.Homes["home-2"].WorkOrders["forge"].ReadyAt=1;store.SaveWorld(shared,[]);
        world=new World(d,g,store);world.JoinCharacter(store.Get(c.Id)!);seq=0;Act("home_work_collect","forge");Act("home_work_cancel","forge");
        Check(store.LoadWorld().Homes["home-2"].WorkOrders.ContainsKey("forge")&&!store.Get(c.Id)!.State.Inventory.ContainsKey("iron_sword"),"full inventory preserves escrow for collection and cancellation");world.Leave(c.Id);
        saved=store.Get(c.Id)!;saved.State.Inventory["potion"]-=g.Items["potion"].Stack;store.Save(saved);
        world=new World(d,g,store);world.JoinCharacter(store.Get(c.Id)!);seq=0;Act("home_work_collect","forge");Act("home_work_collect","forge");Check(store.Get(c.Id)!.State.Inventory["iron_sword"]==1&&!store.LoadWorld().Homes["home-2"].WorkOrders.ContainsKey("forge"),"freeing capacity permits exactly one recovery");world.Leave(c.Id);
        shared=store.LoadWorld();if(previous is null)shared.Homes.Remove("home-2");else shared.Homes["home-2"]=previous;store.SaveWorld(shared,[]);
    }
}
