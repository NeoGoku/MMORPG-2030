using MMORPG.Server;
public static class M4ReviewTests
{
    public static void Run()
    {
        var errors=new List<string>();void Check(bool ok,string label){Console.WriteLine((ok?"PASS":"FAIL")+" M4 review: "+label);if(!ok)errors.Add(label);}
        var data=GameCatalog.Load("Client/data/game.json");var district=District.Load("Client/data/district.json");
        foreach(var cls in new[]{"Dual Swords","Archer"}){
            var w=new World(district,data);var c=new CharacterRecord{ClassId=cls,State=new CharacterState{Level=100,X=-6,Z=-7}};var id=w.JoinCharacter(c)!;
            w.Act(id,1,"skill",cls=="Archer"?"skill7":"skill5");
            Check(cls=="Archer"?w.View(id).Players[0].Z>-7:w.View(id).Players[0].Z< -7,"directional "+cls+" movement skill");
        }
        var world=new World(district,data);var tank=world.JoinCharacter(new CharacterRecord{ClassId="Tank",State=new CharacterState{Level=100,X=-7,Z=-7}})!;var ally=world.JoinCharacter(new CharacterRecord{State=new CharacterState{X=-6,Z=-7}})!;
        world.Act(tank,1,"target","drone-1");world.Act(tank,2,"skill","skill4");world.Act(ally,1,"target","drone-1");world.Act(ally,2,"skill","skill1");
        for(var i=0;i<10;i++)world.Step(.05);
        var view=world.View(tank);Check(view.Players.Single(p=>p.Id==tank).Hp<100&&view.Players.Single(p=>p.Id==ally).Hp==100,"active tank taunt survives ally damage");
        if(errors.Count>0)throw new Exception(string.Join("; ",errors));
    }
}
