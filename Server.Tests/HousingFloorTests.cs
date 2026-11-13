using MMORPG.Server;
public static class HousingFloorTests
{
    public static void Run()
    {
        var catalog=GameCatalog.Load("Client/data/game.json");var district=District.Load("Client/data/district.json");
        var memory=new MemoryGameStore();var connection=Environment.GetEnvironmentVariable("MMORPG_TEST_DATABASE");
        using var postgres=connection is null?null:new PostgresGameStore(connection);IGameStore store=postgres is null?memory:postgres;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS housing floors: "+text);}
        CharacterRecord Make(){var account=Guid.NewGuid().ToString("N");store.Register(new(account,account,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=account,State=new(){X=33,Z=37,Coins=1000,Inventory=new(){{"wood",100},{"oak_chair",2}}}};PetRules.Normalize(c);store.Create(c);return c;}
        var owner=Make();var visitor=Make();var shared=store.LoadWorld();
        // Reuse one physical test apartment while preserving any prior aggregate content.
        var previous=shared.Homes.GetValueOrDefault("home-1");shared.Homes["home-1"]=new(){Owner=owner.Id,Rooms=2,RentUntil=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+3600};store.SaveWorld(shared,[]);
        var world=new World(district,catalog,store);world.JoinCharacter(owner);long seq=0;
        void Act(string kind,string text="")=>world.Act(owner.Id,++seq,kind,"home-1",text);
        Act("home_floor");Act("home_floor");Act("home_floor");
        Check(store.LoadWorld().Homes["home-1"].Floors==3&&store.Get(owner.Id)!.State.Coins==820,"bounded floor extensions charge configured costs exactly once");
        Act("home_visit",visitor.Id+"|allow");world.JoinCharacter(visitor);
        Act("home_stairs","2");Check(store.Get(owner.Id)!.State.Floor==0,"cannot skip a floor");
        Act("home_stairs","1");world.Act(visitor.Id,1,"home_stairs","home-1","1");
        Check(world.View(owner.Id).Players.Where(p=>p.Id==owner.Id||p.Id==visitor.Id).All(p=>p.Floor==1&&p.Elevation==catalog.HousingFloorHeight),"two players synchronize on one physical upper floor");
        Act("home_place","oak_chair|36|32|0");var upstairs=store.LoadWorld().Homes["home-1"].Furniture.Single().Key;
        Act("home_stairs","0");Act("home_place","oak_chair|36|32|0");Act("home_remove",upstairs);
        Check(store.LoadWorld().Homes["home-1"].Furniture.Count==2,"same XZ on different floors is distinct and cannot be removed remotely");
        Act("home_stairs","1");Act("home_remove",upstairs);Act("home_remove",upstairs);
        Check(store.Get(owner.Id)!.State.Inventory.GetValueOrDefault("oak_chair")==1,"upper-floor furniture returns once");
        Act("home_visit",visitor.Id+"|remove");Check(world.View(visitor.Id).Players.Single(p=>p.Id==visitor.Id).Floor==0,"revoked upper-floor visitor is ejected to ground world");
        world.Leave(visitor.Id);Act("home_visit",visitor.Id+"|allow");var lower=store.Get(visitor.Id)!;lower.State.X=33;lower.State.Z=37;store.Save(lower);world.JoinCharacter(lower);
        world.Act(owner.Id,++seq,"trade_open",visitor.Id);Check(System.Text.Json.JsonSerializer.Serialize(world.View(owner.Id).Social).Contains("\"trade\":null"),"cross-floor trade cannot use overlapping XZ");
        world.Leave(owner.Id);world.Leave(visitor.Id);world=new World(district,catalog,store);world.JoinCharacter(store.Get(owner.Id)!);
        Check(world.View(owner.Id).Players.Single().Floor==1&&store.LoadWorld().Homes["home-1"].Floors==3,"restart restores floor occupancy and expanded home");
        world.Input(owner.Id,1,0,1);for(var i=0;i<40;i++){world.Input(owner.Id,i+2,0,1);world.Step(.1);}
        Check(world.View(owner.Id).Players.Single().Z<40,"upper-floor movement cannot walk into outside air");world.Leave(owner.Id);
        shared=store.LoadWorld();if(previous is null)shared.Homes.Remove("home-1");else shared.Homes["home-1"]=previous;store.SaveWorld(shared,[]);
    }
}
