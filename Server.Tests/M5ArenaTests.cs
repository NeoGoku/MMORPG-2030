using MMORPG.Server;
using System.Text.Json;
public static class M5ArenaTests
{
    public static void Run(){
        void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS M5 arena: "+label);}
        var store=new MemoryGameStore();var data=GameCatalog.Load("Client/data/game.json");var district=District.Load("Client/data/district.json");
        CharacterRecord Hero(int stat){var a=Guid.NewGuid().ToString("N");store.Register(new(a,a,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=a,State=new CharacterState{X=-50,Z=35}};c.State.Attributes["strength"]=stat;c.State.Attributes["vitality"]=stat;store.Create(c);return c;}
        var w=new World(district,data,store);var a=w.JoinCharacter(Hero(100000))!;var b=w.JoinCharacter(Hero(0))!;long sa=0,sb=0;
        w.Act(a,++sa,"arena_invite",b);w.Act(b,++sb,"arena_accept",a);
        var json=new JsonSerializerOptions(JsonSerializerDefaults.Web);
        JsonElement Arena()=>JsonSerializer.SerializeToElement(w.View(a),json).GetProperty("arena");
        Check(Arena().GetProperty("matches").GetArrayLength()==1,"consensual duel starts only in arena");
        w.Act(a,++sa,"arena_hit",b);w.Act(a,++sa,"arena_hit",b);
        Check(Arena().GetProperty("matches")[0].GetProperty("hp").GetProperty(b).GetInt32()==85,"huge PvE stats do not affect normalized damage and cooldown");
        for(var i=0;i<6;i++){w.Step(.85);w.Act(a,++sa,"arena_hit",b);}
        Check(Arena().GetProperty("matches").GetArrayLength()==0&&store.Get(a)!.State.Inventory.GetValueOrDefault("arena_token")==1,"duel grants one atomic gameplay reward");
        w.Act(a,++sa,"arena_invite",b);w.Act(b,++sb,"arena_accept",a);
        for(var i=0;i<7;i++){w.Step(.85);w.Act(a,++sa,"arena_hit",b);}
        Check(store.Get(a)!.State.Inventory.GetValueOrDefault("arena_token")==1,"daily account reward guard prevents repeat farming");
        w.Leave(b);var outside=store.Get(b)!;outside.State.X=0;outside.State.Z=10;store.Save(outside);w.JoinCharacter(outside);sb=0;
        w.Act(a,++sa,"arena_invite",b);w.Act(b,++sb,"arena_accept",a);Check(Arena().GetProperty("matches").GetArrayLength()==0,"outside-zone PvP cannot start");w.Leave(a);w.Leave(b);
        var teams=new World(district,data,store);var members=Enumerable.Range(0,4).Select(_=>teams.JoinCharacter(Hero(0))!).ToArray();var sequence=members.ToDictionary(id=>id,_=>0L);
        void TeamAct(int index,string kind,string target="")=>teams.Act(members[index],++sequence[members[index]],kind,target);
        TeamAct(0,"party_invite",members[1]);TeamAct(1,"party_accept",members[0]);TeamAct(2,"party_invite",members[3]);TeamAct(3,"party_accept",members[2]);
        for(var i=0;i<4;i++)TeamAct(i,"arena_ready");TeamAct(0,"arena_team_invite",members[2]);TeamAct(2,"arena_accept",members[0]);
        JsonElement TeamArena()=>JsonSerializer.SerializeToElement(teams.View(members[0]),json).GetProperty("arena");
        Check(TeamArena().GetProperty("matches")[0].GetProperty("mode").GetString()=="team","two consensual ready parties enter team match");
        TeamAct(0,"arena_hit",members[1]);Check(TeamArena().GetProperty("matches")[0].GetProperty("hp").GetProperty(members[1]).GetInt32()==100,"friendly team attacks are rejected");TeamAct(0,"arena_leave");
        Check(TeamArena().GetProperty("matches").GetArrayLength()==0&&store.Get(members[2])!.State.Inventory.GetValueOrDefault("arena_token")==0,"team forfeit ends match without farmable rewards");foreach(var id in members)teams.Leave(id);
    }
}
