using MMORPG.Server;
using System.Text.Json;
public static class M3Regression
{
    public static void Run()
    {
        var errors=new List<string>();
        void Check(bool ok,string name){Console.WriteLine((ok?"PASS":"FAIL")+" M3 regression: "+name);if(!ok)errors.Add(name);}
        var catalog=GameCatalog.Load("Client/data/game.json");var district=District.Load("Client/data/district.json");
        var store=new FailingStore();var record=new CharacterRecord{AccountId="owner",State=new CharacterState{X=8,Z=6,Coins=100}};store.Create(record);
        var world=new World(district,catalog,store);var id=world.JoinCharacter(record)!;
        var snapshot=world.View(id).Self.Character!;
        world.Act(id,1,"buy","potion");
        Check(JsonSerializer.SerializeToElement(snapshot).GetProperty("inventory").GetProperty("potion").GetInt32()==2,"snapshot detached from future inventory changes");
        world.Leave(id);var stale=store.Get(id)!;var fresh=store.Get(id)!;fresh.State.Coins=321;store.Save(fresh);
        world.JoinCharacter(stale);
        Check(world.View(id).Self.Coins==321,"join reloads latest committed character after stale ticket read");
        world.Leave(id);
        var mage=new CharacterRecord{AccountId="owner",Slot=1,ClassId="Dark Mage",State=new CharacterState{X=-6,Z=-7}};store.Create(mage);var mid=world.JoinCharacter(mage)!;world.Act(mid,1,"target","drone-1");
        var before=world.View(mid);store.Fail=true;world.Act(mid,2,"skill","skill1");var after=world.View(mid);
        Check(after.Monsters[0].Hp==before.Monsters[0].Hp&&JsonSerializer.SerializeToElement(after.Self.Character).GetProperty("mana").GetInt32()==100,"failed save rolls back combat and resource outcomes together");
        store.Fail=false;
        world.Act(mid,3,"skill","skill1");world.Act(mid,4,"pet","combat");
        world.Step(.05);var hpSaved=world.View(mid).Players.Single(p=>p.Id==mid).Hp;
        world.Leave(mid);world.JoinCharacter(store.Get(mid)!);world.Act(mid,1,"target","drone-1");
        var monsterHp=world.View(mid).Monsters[0].Hp;world.Act(mid,2,"skill","skill1");
        Check(world.View(mid).Monsters[0].Hp==monsterHp,"skill cooldown survives reconnect");
        Check(world.View(mid).Players.Single(p=>p.Id==mid).Hp==hpSaved,"reconnect preserves HP instead of healing");
        if(errors.Count>0)throw new Exception(string.Join("; ",errors));
    }
    private sealed class FailingStore:IGameStore
    {
        private readonly MemoryGameStore inner=new();public bool Fail;public bool Durable=>false;
        public AccountRecord? Account(string u)=>inner.Account(u);public void Register(AccountRecord a)=>inner.Register(a);public List<CharacterRecord> List(string a)=>inner.List(a);public CharacterRecord? Get(string id)=>inner.Get(id);public void Create(CharacterRecord c)=>inner.Create(c);public void SaveBatch(IReadOnlyList<CharacterRecord> c){if(Fail)throw new GameRuleException("Simulated failure");inner.SaveBatch(c);}
        public void Save(CharacterRecord c){if(Fail)throw new GameRuleException("Simulated failure");inner.Save(c);}
    }
}
