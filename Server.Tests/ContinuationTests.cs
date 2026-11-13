using MMORPG.Server;
public static class ContinuationTests
{
 public static void Run(){
  void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS continuation: "+text);}
  var g=GameCatalog.Load("Client/data/game.json");var c=new CharacterRecord();c.State.Inventory["iron_sword"]=1;
  Check(GearRules.Unpack(c.State,"iron_sword",g),"unpack creates distinct item without duplicating inventory");
  var id=c.State.Gear.Keys.Single();Check(!c.State.Inventory.ContainsKey("iron_sword")&&GearRules.Equip(c,id,g),"owned compatible gear equips");
  c.State.Gear[id].Condition=0;var damaged=CharacterRules.Attack(c,g);c.State.Coins=100;
  Check(GearRules.Repair(c.State,id,g)&&CharacterRules.Attack(c,g)>damaged,"repair restores actual gear contribution");
  Check(AppearanceRules.Valid("female|2|1|3|2|1" )&&!AppearanceRules.Valid("bad|99|0|0|0|0"),"appearance bounds reject malformed choices");
 }
}
