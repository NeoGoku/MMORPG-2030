using MMORPG.Server;
using System.Text.Json;
public static class M5ReviewTests
{
    public static void Run(){
        var failures=new List<string>();void Check(bool ok,string name){Console.WriteLine((ok?"PASS":"FAIL")+" M5 review: "+name);if(!ok)failures.Add(name);}
        var d=District.Load("Client/data/district.json");var g=GameCatalog.Load("Client/data/game.json");var json=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        d.Encounters.Single(e=>e.Id=="worldboss-1").Hp=1;var w=new World(d,g);var hero=new CharacterRecord{State=new CharacterState{X=0,Z=-74,Level=100,Pet=new PetState{Active=true,Mode="loot"}}};var a=w.JoinCharacter(hero)!;
        w.Act(a,1,"target","worldboss-1");w.Act(a,2,"skill","skill1");w.Step(.05);var inventory=JsonSerializer.SerializeToElement(w.View(a).Self.Character,json).GetProperty("inventory");
        Check(inventory.TryGetProperty("boss_core",out var core)&&core.GetInt32()==1,"loot companion preserves boss core reward");
        var store=new MemoryGameStore();CharacterRecord Make(){var account=Guid.NewGuid().ToString("N");store.Register(new(account,account,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=account,State=new CharacterState{X=36,Z=34,Inventory=new(){{"ore",4}}}};c.State.ClaimedQuests.Add("herbs");store.Create(c);return c;}
        var owner=Make();var peer=Make();var shared=store.LoadWorld();var guild=new GuildState{Name="Test",Leader=owner.Id,Members=new(){{owner.Id,"leader"}}};for(var i=0;i<50;i++)guild.Invitations["expired"+i]=1;shared.Guilds["test"]=guild;
        shared.Homes["home-1"]=new HomeState{Owner=owner.Id,RentUntil=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+3600};store.SaveWorld(shared,[]);
        var online=new World(District.Load("Client/data/district.json"),g,store);var id=online.JoinCharacter(owner)!;online.Act(id,1,"guild_invite",peer.Id);
        Check(store.LoadWorld().Guilds["test"].Invitations.ContainsKey(peer.Id),"expired invitations release capacity");
        online.Act(id,2,"home_craft","home-1","iron_sword");Check(store.Get(id)!.State.QuestProgress.GetValueOrDefault("artisan")==1,"home workshop advances existing crafting quest");
        online.Leave(id);if(failures.Count>0)throw new Exception(string.Join("; ",failures));
    }
}
