using MMORPG.Server;
public static class ProductionTests
{
 public static void Run(){
  void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS production: "+name);}
  var store=new MemoryGameStore();var account=Guid.NewGuid().ToString("N");store.Register(new(account,account,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=account,State=new CharacterState{X=36,Z=34,Inventory=new(){{"ore",5}}}};store.Create(c);var shared=store.LoadWorld();shared.Homes["home-1"]=new HomeState{Owner=c.Id,RentUntil=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+3600,Furniture=new(){{"table",new FurnitureState{Item="oak_table",X=34,Z=32}}}};store.SaveWorld(shared,[]);
  var g=GameCatalog.Load("Client/data/game.json");var d=District.Load("Client/data/district.json");var world=new World(d,g,store);world.JoinCharacter(c);world.Act(c.Id,1,"home_produce","home-1","iron_sword");world.Act(c.Id,2,"home_collect","home-1");Check(store.Get(c.Id)!.State.Inventory["ore"]==2&&!store.Get(c.Id)!.State.Inventory.ContainsKey("iron_sword"),"production escrows materials and rejects premature collection");world.Leave(c.Id);
  shared=store.LoadWorld();shared.Homes["home-1"].Production!.ReadyAt=1;store.SaveWorld(shared,[]);world=new World(d,g,store);world.JoinCharacter(store.Get(c.Id)!);world.Act(c.Id,1,"home_collect","home-1");world.Act(c.Id,2,"home_collect","home-1");Check(store.Get(c.Id)!.State.Inventory["iron_sword"]==1&&store.LoadWorld().Homes["home-1"].Production is null,"recreated world grants offline output only once");world.Leave(c.Id);
  world=new World(d,g,store);world.JoinCharacter(store.Get(c.Id)!);world.Act(c.Id,1,"home_produce","home-1","iron_sword");
  // Not enough ore remains: failed start must not invent an escrow/refund.
  world.Act(c.Id,2,"home_cancel","home-1");Check(store.Get(c.Id)!.State.Inventory["ore"]==2,"failed production cannot refund materials");world.Leave(c.Id);
  var saved=store.Get(c.Id)!;saved.State.Inventory["ore"]=5;store.Save(saved);world=new World(d,g,store);world.JoinCharacter(saved);world.Act(c.Id,1,"home_produce","home-1","iron_sword");world.Leave(c.Id);
  // Refund original escrow even if a later catalogue changes the recipe.
  g.Recipes["iron_sword"].Inputs["ore"]=99;world=new World(d,g,store);world.JoinCharacter(store.Get(c.Id)!);world.Act(c.Id,1,"home_cancel","home-1");world.Act(c.Id,2,"home_cancel","home-1");
  Check(store.Get(c.Id)!.State.Inventory["ore"]==5&&store.Get(c.Id)!.State.Inventory["iron_sword"]==1&&store.LoadWorld().Homes["home-1"].Production is null,"restart cancellation returns original escrow once despite recipe changes");
  world.Leave(c.Id);
 }
}
