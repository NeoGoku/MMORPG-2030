using MMORPG.Server;
public static class StallTests
{
 public static void Run(){
  void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS stalls: "+name);}
  var memory=new MemoryGameStore();var connection=Environment.GetEnvironmentVariable("MMORPG_TEST_DATABASE");IGameStore Open()=>connection is null?memory:new PostgresGameStore(connection);var store=Open();var g=GameCatalog.Load("Client/data/game.json");var d=District.Load("Client/data/district.json");
  CharacterRecord Make(double x,double z){var account=Guid.NewGuid().ToString("N");store.Register(new(account,account,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=account,State=new CharacterState{X=x,Z=z,Coins=100}};store.Create(c);return c;}
  var a=Make(30,18);var b=Make(0,10);g.MaxStalls=1;var expiredKey="expired_"+a.Id;var initial=store.LoadWorld();initial.Stalls[expiredKey]=new StallState{X=30,Z=18,Title="Alt",Until=1};store.SaveWorld(initial,[]);var world=new World(d,g,store);world.JoinCharacter(a);world.JoinCharacter(b);world.Act(a.Id,1,"stall_open","","Handwerk");Check(store.LoadWorld().Stalls.ContainsKey(a.Id),"expired stand does not consume active capacity");world.Act(a.Id,2,"stall_list","","potion|1|10");var listing=store.LoadWorld().Auctions.Single(p=>p.Value.StallId==a.Id).Key;
  world.Act(b.Id,1,"stall_buy",listing);Check(store.Get(b.Id)!.State.Coins==100&&store.LoadWorld().Auctions.ContainsKey(listing),"remote purchase cannot consume escrow or money");world.Leave(a.Id);world.Leave(b.Id);(store as IDisposable)?.Dispose();store=Open();
  var buyer=store.Get(b.Id)!;buyer.State.X=31;buyer.State.Z=18;store.Save(buyer);world=new World(d,g,store);world.JoinCharacter(buyer);world.Act(b.Id,1,"stall_buy",listing);world.Act(b.Id,2,"stall_buy",listing);world.Leave(b.Id);
  Check(store.Get(b.Id)!.State.Inventory["potion"]==3&&store.Get(b.Id)!.State.Coins==90&&store.LoadWorld().Proceeds[a.Id]==9,"offline seller stall survives reopen and purchases once");
  world.JoinCharacter(store.Get(a.Id)!);world.Act(a.Id,1,"stall_claim","");world.Act(a.Id,2,"stall_list","","potion|1|5");var remaining=store.LoadWorld().Auctions.Single(p=>p.Value.StallId==a.Id).Key;world.Act(a.Id,3,"stall_close","");world.Leave(a.Id);
  Check(!store.LoadWorld().Stalls.ContainsKey(a.Id)&&store.LoadWorld().Auctions[remaining].StallId==""&&store.Get(a.Id)!.State.Coins==109,"closing conserves remaining exchange offers and seller proceeds");
  var owner=store.Get(a.Id)!;owner.State.X=30;owner.State.Z=12;store.Save(owner);world=new World(d,g,store);world.JoinCharacter(owner);world.Act(a.Id,1,"market_cancel",remaining);world.Act(a.Id,2,"market_cancel",remaining);world.Leave(a.Id);
  Check(store.Get(a.Id)!.State.Inventory["potion"]==1&&!store.LoadWorld().Auctions.ContainsKey(remaining),"remaining stock reclaimed once after close");var cleanup=store.LoadWorld();cleanup.Stalls.Remove(expiredKey);store.SaveWorld(cleanup,[]);(store as IDisposable)?.Dispose();
 }
}
