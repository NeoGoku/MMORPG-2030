using MMORPG.Server;
public static class DurableInstanceTests
{
 public static void Run(){
  void Check(bool v,string name){if(!v)throw new Exception(name);Console.WriteLine("PASS durable instances: "+name);}
  var g=GameCatalog.Load("Client/data/game.json");var d=District.Load("Client/data/district.json");var memory=new MemoryGameStore();var connection=Environment.GetEnvironmentVariable("MMORPG_TEST_DATABASE");IGameStore Open()=>connection is null?memory:new PostgresGameStore(connection);
  var store=Open();var suffix=Guid.NewGuid().ToString("N");CharacterRecord Make(string name){var account=Guid.NewGuid().ToString("N");store.Register(new(account,"durable_"+account,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=account,Name=name,State=new CharacterState{X=30,Z=24,Coins=100,Inventory=new(){{"iron_sword",1}}}};store.Create(c);return c;}
  var a=Make("Leitung");var b=Make("Mitglied");GearRules.Unpack(b.State,"iron_sword",g);var id=b.State.Gear.Keys.Single();b.State.Gear[id].Condition=17;store.Save(b);
  var shared=store.LoadWorld();var gid="durable_"+suffix;shared.Guilds[gid]=new GuildState{Name=gid,Leader=a.Id,Members=new(){{a.Id,"leader"},{b.Id,"member"}}};store.SaveWorld(shared,[]);
  var world=new World(d,g,store);world.JoinCharacter(a);world.JoinCharacter(b);world.Act(b.Id,1,"guild_gear_put",id);world.Act(b.Id,2,"guild_gear_take",id);
  Check(!store.Get(b.Id)!.State.Gear.ContainsKey(id)&&store.LoadWorld().Guilds[gid].GearStorage[id].Condition==17,"member deposit persists and unauthorized withdrawal changes nothing");
  world.Act(a.Id,1,"guild_rank",b.Id,"officer");world.Leave(a.Id);world.Leave(b.Id);(store as IDisposable)?.Dispose();store=Open();
  world=new World(d,g,store);world.JoinCharacter(store.Get(b.Id)!);world.Act(b.Id,1,"guild_gear_take",id);world.Act(b.Id,2,"guild_gear_take",id);
  Check(store.Get(b.Id)!.State.Gear.Count(x=>x.Key==id)==1&&store.Get(b.Id)!.State.Gear[id].Condition==17&&!store.LoadWorld().Guilds[gid].GearStorage.ContainsKey(id),"store reopen restores rank and single instance with original wear");world.Leave(b.Id);
  var seller=store.Get(b.Id)!;seller.State.X=30;seller.State.Z=12;store.Save(seller);world=new World(d,g,store);world.JoinCharacter(seller);world.Act(b.Id,1,"market_list","",id+"|1|12");var listing=store.LoadWorld().Auctions.Single(x=>x.Value.GearId==id).Key;world.Leave(b.Id);(store as IDisposable)?.Dispose();store=Open();
  var buyer=store.Get(a.Id)!;buyer.State.X=30;buyer.State.Z=12;buyer.State.Inventory=new(){{"potion",g.InventorySlots*g.Items["potion"].Stack}};store.Save(buyer);world=new World(d,g,store);world.JoinCharacter(buyer);world.Act(a.Id,1,"market_buy",listing);
  Check(store.Get(a.Id)!.State.Coins==100&&store.LoadWorld().Auctions.ContainsKey(listing),"full inventory after restart preserves escrow and buyer currency");world.Leave(a.Id);
  buyer=store.Get(a.Id)!;buyer.State.Inventory.Clear();store.Save(buyer);world=new World(d,g,store);world.JoinCharacter(buyer);world.Act(a.Id,1,"market_buy",listing);world.Act(a.Id,2,"market_buy",listing);world.Leave(a.Id);
  Check(store.Get(a.Id)!.State.Gear[id].Condition==17&&store.Get(a.Id)!.State.Coins==88&&!store.LoadWorld().Auctions.ContainsKey(listing),"restarted auction transfers gear and currency once");
  var restored=store.Get(a.Id)!;Check(!restored.State.Inventory.ContainsKey("iron_sword")&&store.Get(b.Id)!.State.Inventory.GetValueOrDefault("iron_sword")==0,"legacy stack conversion does not reappear on reload");
  shared=store.LoadWorld();shared.Guilds.Remove(gid);store.SaveWorld(shared,[]);(store as IDisposable)?.Dispose();
 }
}
