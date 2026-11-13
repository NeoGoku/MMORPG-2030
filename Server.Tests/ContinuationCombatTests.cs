using MMORPG.Server;
using System.Text.Json;
public static class ContinuationCombatTests
{
 public static void Run(){
  var g=GameCatalog.Load("Client/data/game.json");var d=District.Load("Client/data/district.json");var json=new JsonSerializerOptions(JsonSerializerDefaults.Web);var count=0;
  foreach(var kit in g.Classes)foreach(var skill in kit.Value.Skills){
   var store=new MemoryGameStore();CharacterRecord Create(string cls){var account=Guid.NewGuid().ToString("N");store.Register(new(account,account,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=account,ClassId=cls,State=new CharacterState{X=-50,Z=35,Level=100}};c.State.Attributes["strength"]=100000;store.Create(c);return c;}
   var a=Create(kit.Key);var b=Create("Tank");var w=new World(d,g,store);w.JoinCharacter(a);w.JoinCharacter(b);long seq=1;
   w.Act(a.Id,seq,"arena_invite",b.Id);w.Act(b.Id,1,"arena_accept",a.Id);w.Act(b.Id,2,"arena_hit",a.Id);
   if(skill.Kind=="explode")w.Act(a.Id,++seq,"arena_skill","skill3",b.Id);
   w.Act(a.Id,++seq,"arena_skill",skill.Id,b.Id);
   var match=JsonSerializer.SerializeToElement(w.View(a.Id).Arena,json).GetProperty("matches")[0];
   var expected=skill.Kind=="explode"?60:80;if(match.GetProperty("energy").GetProperty(a.Id).GetInt32()!=expected)throw new Exception("Arena effect not executed: "+kit.Key+"/"+skill.Id);
   if(skill.Kind is "damage" or "dot" or "slow" or "break" or "drain" or "taunt" or "aoe")if(match.GetProperty("hp").GetProperty(b.Id).GetInt32()!=100-skill.PvpPower)throw new Exception("PvE stats leaked into PvP damage");
   if(skill.Kind is "heal" or "party_heal" or "hot")if(match.GetProperty("hp").GetProperty(a.Id).GetInt32()<=85)throw new Exception("Arena healing missing");
   count++;w.Leave(a.Id);w.Leave(b.Id);
  }
  Console.WriteLine("PASS continuation combat: "+count+" normalized class skills spend energy and apply damage/healing without PvE stat leakage");
 }
}
