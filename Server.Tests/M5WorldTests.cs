using MMORPG.Server;
using System.Text.Json;
public static class M5WorldTests
{
    public static void Run(){
        void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS M5 world: "+label);}
        var d=District.Load("Client/data/district.json");var g=GameCatalog.Load("Client/data/game.json");var w=new World(d,g);var a=w.JoinCharacter(new CharacterRecord{State=new CharacterState{X=0,Z=-70,Level=10}})!;
        var snapshot=JsonSerializer.SerializeToElement(w.View(a),new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Check(snapshot.GetProperty("monsters").EnumerateArray().Any(m=>m.GetProperty("id").GetString()=="worldboss-1"&&m.GetProperty("maxHp").GetInt32()>500),"open dungeon contains a real high-HP worldboss");
        for(var i=0;i<30;i++)w.Step(.05);
        snapshot=JsonSerializer.SerializeToElement(w.View(a),new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Check(snapshot.GetProperty("monsters").EnumerateArray().Any(m=>m.GetProperty("id").GetString()=="worldboss-1"&&m.GetProperty("warning").GetDouble()>0),"boss publishes avoidable telegraph before area damage");w.Leave(a);
    }
}
