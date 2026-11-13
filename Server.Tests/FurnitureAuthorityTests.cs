using MMORPG.Server;
public static class FurnitureAuthorityTests
{
 public static void Run(){
  void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS furniture/vendor: "+label);}
  var g=GameCatalog.Load("Client/data/game.json");var state=new CharacterState{Coins=1000};Check(!CharacterRules.Buy(state,"arena_token",g)&&!CharacterRules.Buy(state,"boss_core",g)&&state.Coins==1000&&!state.Inventory.ContainsKey("arena_token"),"forged vendor requests cannot mint rewards or boss materials");Check(CharacterRules.Buy(state,"potion",g),"normal vendor stock remains buyable");
  var store=new MemoryGameStore();var account=Guid.NewGuid().ToString("N");store.Register(new(account,account,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=account,State=new CharacterState{X=36,Z=34,Inventory=new(){{"oak_table",1}}}};store.Create(c);var shared=store.LoadWorld();shared.Homes["home-1"]=new HomeState{Owner=c.Id,RentUntil=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+3600};store.SaveWorld(shared,[]);
  var w=new World(District.Load("Client/data/district.json"),g,store);w.JoinCharacter(c);w.Act(c.Id,1,"home_place","home-1","oak_table|36|34|0");Check(store.Get(c.Id)!.State.Inventory["oak_table"]==1&&store.LoadWorld().Homes["home-1"].Furniture.Count==0,"placement cannot enclose an occupant");
  w.Act(c.Id,2,"home_place","home-1","oak_table|34|32|1");var placed=store.LoadWorld().Homes["home-1"].Furniture.Single();Check(placed.Value.Item=="oak_table"&&!store.Get(c.Id)!.State.Inventory.ContainsKey("oak_table"),"placed furniture retains actual crafted item type");w.Act(c.Id,3,"home_remove","home-1",placed.Key);w.Act(c.Id,4,"home_remove","home-1",placed.Key);Check(store.Get(c.Id)!.State.Inventory["oak_table"]==1,"table removal returns exact item once");w.Leave(c.Id);
 }
}
