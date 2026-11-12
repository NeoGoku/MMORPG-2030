using MMORPG.Server;
using System.Text.Json;
public static class M4Tests
{
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    public static void Run()
    {
        void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS M4: "+label);}
        var data=GameCatalog.Load("Client/data/game.json");var district=District.Load("Client/data/district.json");var world=new World(district,data);
        var ids=Enumerable.Range(0,11).Select(_=>world.Join()!).ToArray();var seq=ids.ToDictionary(x=>x,_=>0L);
        void Act(string who,string kind,string target="",string text="")=>world.Act(who,++seq[who],kind,target,text);
        JsonElement Social(string who)=>JsonSerializer.SerializeToElement(world.View(who).Social,Json);
        for(var i=1;i<11;i++){Act(ids[0],"party_invite",ids[i]);Act(ids[i],"party_accept",ids[0]);}
        Check(Social(ids[0]).GetProperty("party").GetProperty("members").GetArrayLength()==10,"party cap is ten including leader");
        Check(Social(ids[10]).GetProperty("party").ValueKind==JsonValueKind.Null,"eleventh member rejected");
        Act(ids[0],"chat_private",ids[1],"Nur Empfänger B");
        Check(Social(ids[1]).GetProperty("chat").GetArrayLength()==1&&Social(ids[2]).GetProperty("chat").GetArrayLength()==0,"private chat visible only to intended participants");
        Act(ids[1],"party_kick",ids[2]);Check(Social(ids[2]).GetProperty("party").ValueKind!=JsonValueKind.Null,"nonleader cannot kick");
        Act(ids[0],"party_promote",ids[1]);Act(ids[1],"party_kick",ids[2]);Check(Social(ids[2]).GetProperty("party").ValueKind==JsonValueKind.Null,"leader transfer and kick");
        world.Step(1);Act(ids[0],"chat_party","","Gruppenkanal");
        Check(!Social(ids[2]).GetProperty("chat").EnumerateArray().Any(x=>x.GetProperty("text").GetString()=="Gruppenkanal"),"party chat excludes kicked players");
        Check(Social(ids[1]).GetProperty("chat").EnumerateArray().Any(x=>x.GetProperty("text").GetString()=="Gruppenkanal"),"party members receive party chat");
        var partyWorld=new World(district,data);var leader=new CharacterRecord{State=new CharacterState{X=-7,Z=-6,Quest="active"}};var member=new CharacterRecord{State=new CharacterState{X=-6,Z=-6,Quest="active"}};
        var la=partyWorld.JoinCharacter(leader)!;var lb=partyWorld.JoinCharacter(member)!;
        partyWorld.Act(la,1,"party_invite",lb);partyWorld.Act(lb,1,"party_accept",la);
        partyWorld.Act(la,2,"target","drone-1");partyWorld.Act(la,3,"skill","skill1");for(var i=0;i<65;i++)partyWorld.Step(.05);partyWorld.Act(la,4,"skill","skill1");
        Check(partyWorld.View(la).Self.Exp==10&&partyWorld.View(lb).Self.Exp==10,"shared nearby party EXP conserves kill pool");
        Check(partyWorld.View(la).Loot.Length==1&&partyWorld.View(lb).Loot.Length==1&&partyWorld.View(lb).Self.Kills==1,"party kill credit and personal drops for both");
        foreach(var cls in data.Classes)foreach(var skill in cls.Value.Skills){
            var w=new World(district,data);var hero=new CharacterRecord{ClassId=cls.Key,State=new CharacterState{Level=100,X=-6,Z=-7,Hp=40}};var h=w.JoinCharacter(hero)!;
            w.Act(h,1,"target","drone-1");if(skill.Kind=="explode")w.Act(h,2,"skill","skill3");w.Act(h,3,"skill",skill.Id);
            var state=JsonSerializer.SerializeToElement(w.View(h).Self.Character,Json);
            Check(state.GetProperty("mana").GetInt32()<100,"real resource-consuming skill "+cls.Key+"/"+skill.Id);
        }
        IGameStore marketStore=Environment.GetEnvironmentVariable("MMORPG_TEST_DATABASE") is {} db?new PostgresGameStore(db):new MemoryGameStore();
        var auth=new AccountService(marketStore,data);var suffix=Guid.NewGuid().ToString("N")[..8];
        var accountA=auth.Resolve(auth.Register("tradea_"+suffix,"Trade-Test-Only-Password-123!").Token)!;var accountB=auth.Resolve(auth.Register("tradeb_"+suffix,"Trade-Test-Only-Password-123!").Token)!;
        var seller=new CharacterRecord{Id="a"+Guid.NewGuid().ToString("N")[..31],AccountId=accountA,State=new CharacterState{Coins=10,Inventory=new(){{"ore",3}}}};var buyer=new CharacterRecord{Id="b"+Guid.NewGuid().ToString("N")[..31],AccountId=accountB,State=new CharacterState{Coins=50}};marketStore.Create(seller);marketStore.Create(buyer);
        var tw=new World(district,data,marketStore);var sa=tw.JoinCharacter(seller)!;var sb=tw.JoinCharacter(buyer)!;
        tw.Act(sa,1,"trade_open",sb);tw.Act(sa,2,"trade_offer","","ore|2|0");tw.Act(sb,1,"trade_offer","","|0|12");tw.Act(sa,3,"trade_confirm","");
        Check(marketStore.Get(sa)!.State.Inventory["ore"]==3,"one-sided confirmation cannot transfer");tw.Act(sb,2,"trade_confirm","");
        Check(marketStore.Get(sa)!.State.Coins==22&&marketStore.Get(sb)!.State.Coins==38&&marketStore.Get(sb)!.State.Inventory["ore"]==2,"two-party trade transfers currency/items atomically");
        tw.Act(sb,3,"trade_confirm","");Check(marketStore.Get(sb)!.State.Inventory["ore"]==2,"repeated trade confirmation cannot duplicate items");
        tw.Leave(sa);tw.Leave(sb);
        var oldSeller=marketStore.Get(sa)!;var staleBuyer=marketStore.Get(sb)!;var newerBuyer=marketStore.Get(sb)!;newerBuyer.State.Coins++;marketStore.Save(newerBuyer);
        oldSeller.State.Coins+=5;staleBuyer.State.Coins-=5;
        try{marketStore.SaveBatch([oldSeller,staleBuyer]);throw new Exception("stale trade committed");}catch(GameRuleException){}
        Check(marketStore.Get(sa)!.State.Coins==22,"failed second row rolls back entire multi-character transaction");
        (marketStore as IDisposable)?.Dispose();
        var farmWorld=new World(district,data);var farmHero=new CharacterRecord{State=new CharacterState{X=0,Z=3}};var fid=farmWorld.JoinCharacter(farmHero)!;
        farmWorld.Act(fid,1,"farm_start","14");
        Check(JsonSerializer.SerializeToElement(farmWorld.View(fid).Self.Character,Json).GetProperty("automation").GetProperty("active").GetBoolean(),"guarded automation starts in safe zone");
        farmWorld.Input(fid,2,1,0);
        Check(!JsonSerializer.SerializeToElement(farmWorld.View(fid).Self.Character,Json).GetProperty("automation").GetProperty("active").GetBoolean(),"manual movement cancels automation immediately");
        farmWorld.Act(fid,3,"farm_start","14");Check(!JsonSerializer.SerializeToElement(farmWorld.View(fid).Self.Character,Json).GetProperty("automation").GetProperty("active").GetBoolean(),"restart cannot extend automation cooldown window");
        var distant=farmWorld.JoinCharacter(new CharacterRecord{State=new CharacterState{X=50,Z=50}})!;
        Check(farmWorld.View(fid).Players.All(p=>p.Id!=distant),"distance-interest excludes far world entities");
        var timedDistrict=District.Load("Client/data/district.json");timedDistrict.Rules.MonsterHp=1000000;foreach(var spawn in timedDistrict.Monsters){spawn.Speed=0;spawn.Reach=0;}
        var timedWorld=new World(timedDistrict,data);var timedId=timedWorld.JoinCharacter(new CharacterRecord{State=new CharacterState{X=0,Z=3}})!;timedWorld.Act(timedId,1,"farm_start","14");
        for(var i=0;i<1450;i++)timedWorld.Step(.05);
        Check(!JsonSerializer.SerializeToElement(timedWorld.View(timedId).Self.Character,Json).GetProperty("automation").GetProperty("active").GetBoolean(),"automation stops after bounded combat phase and breadcrumb return");
        Check(data.Classes.All(c=>c.Value.Skills.Count>=8&&c.Value.Skills.Count(s=>s.Enabled)>=8),"eight functional skill definitions per class");
    }
}
