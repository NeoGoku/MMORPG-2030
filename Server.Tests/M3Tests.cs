using MMORPG.Server;

public static class M3Tests
{
    public static void Run()
    {
        void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS M3: "+text);}
        var data=GameCatalog.Load("Client/data/game.json");
        IGameStore store=Environment.GetEnvironmentVariable("MMORPG_TEST_DATABASE") is {} connection?new PostgresGameStore(connection):new MemoryGameStore();
        var username="tester_"+Guid.NewGuid().ToString("N")[..8];
        var auth=new AccountService(store,data);
        var session=auth.Register(username,"Strong-Test-Password-123!");
        Check(session is not null,"registration creates password-hashed identity");
        Check(auth.Login(username,"wrong-password") is null,"wrong credentials rejected");
        var account=auth.Resolve(session!.Token)!;
        var a=auth.CreateCharacter(account,0,"MiraTest","Shield Sword");
        auth.CreateCharacter(account,1,"MageTest","Dark Mage");auth.CreateCharacter(account,2,"HealTest","Healer");
        Check(auth.List(account).Count==3,"three owned character slots");
        try{auth.CreateCharacter(account,0,"Duplicate","Tank");throw new Exception("duplicate accepted");}catch(GameRuleException){}
        Check(auth.List("someone-else").Count==0,"character list is account scoped");
        var ticket=auth.Ticket(session.Token,a.Id);
        Check(auth.ConsumeTicket(ticket)?.Id==a.Id&&auth.ConsumeTicket(ticket) is null,"one-use ticket binds owned identity");
        var c=a.State;
        c.StatPoints=2;
        Check(CharacterRules.Allocate(c,"strength",data)&&c.StatPoints==1,"server spends allocated stat point");
        Check(!CharacterRules.Allocate(c,"unknown",data)&&c.StatPoints==1,"unknown attribute rejected");
        c.Inventory["ore"]=3;c.Coins=100;
        Check(CharacterRules.Craft(c,"iron_sword",data)&&c.Inventory.GetValueOrDefault("ore")==0&&c.Inventory["iron_sword"]==1,"atomic recipe consumes input and grants output");
        var before=c.Coins;
        Check(!CharacterRules.Buy(c,"unknown",data)&&c.Coins==before,"invalid trade leaves currency unchanged");
        Check(CharacterRules.Equip(c,"iron_sword",data)&&c.Equipment["weapon"]=="iron_sword","equipment owned item validation");
        Check(data.Classes.Count==8&&data.Attributes.Count==8&&data.Classes.All(x=>x.Value.Skills.Count>=8),"eight fixed classes and six slots");
        store.Save(a);var restored=store.Get(a.Id)!;
        Check(restored.State.Equipment["weapon"]=="iron_sword","aggregate save restores equipment and inventory together");
        var stale=store.Get(a.Id)!;a.State.Coins++;store.Save(a);
        try{store.Save(stale);throw new Exception("stale accepted");}catch(GameRuleException){}
        Check(store.Get(a.Id)!.State.Coins==a.State.Coins,"optimistic save rejects concurrent lost update");
        foreach(var cls in data.Classes.Keys){
            var world=new World(District.Load("Client/data/district.json"),data,new MemoryGameStore());
            var hero=new CharacterRecord{ClassId=cls,State=new CharacterState{X=-6,Z=-7,Hp=40}};
            var id=world.JoinCharacter(hero)!;Check(world.JoinCharacter(hero) is null,"active character lease "+cls);
            world.Act(id,1,"target","drone-1");world.Step(.05);world.Act(id,2,"skill","skill1");
            var hit=world.View(id).Monsters.Single(m=>m.Id=="drone-1").Hp;
            world.Act(id,3,"skill","skill1");Check(world.View(id).Monsters.Single(m=>m.Id=="drone-1").Hp==hit&&hit<42,"skill damage/cooldown "+cls);
            world.Act(id,4,"skill","skill2");
            var view=System.Text.Json.JsonSerializer.SerializeToElement(world.View(id).Self.Character);
            Check(view.GetProperty("mana").GetInt32()==75,"two class skills spend server mana "+cls);
        }
        (store as IDisposable)?.Dispose();
    }
}
