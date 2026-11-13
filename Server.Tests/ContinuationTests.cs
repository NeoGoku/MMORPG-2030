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
  c.State.Level=5;Check(ClassRules.Defense(c,g)>0,"shield class passive contributes defense at unlock level");
  c.ClassId="Healer";Check(!GearRules.Equip(c,id,g)||g.Items[c.State.Gear[id].Item].Classes.Count==0,"class requirements are authoritative");
  var apprentice=new CharacterState{Inventory=new(){{"ore",10}}};Check(!CharacterRules.Craft(apprentice,"iron_helm",g),"advanced recipe requires profession mastery");apprentice.Professions["smithing"]=100;Check(CharacterRules.Craft(apprentice,"iron_helm",g),"mastery unlock consumes real materials");
  var cells=new SpatialIndex();cells.Rebuild(new[]{("near",0.0,0.0),("far",100.0,100.0)});Check(cells.Query(0,0,10).Contains("near")&&!cells.Query(0,0,10).Contains("far"),"world cells exclude distant candidates");
  Check(AppearanceRules.Valid("female|2|1|3|2|1" )&&!AppearanceRules.Valid("bad|99|0|0|0|0"),"appearance bounds reject malformed choices");
 }
}
