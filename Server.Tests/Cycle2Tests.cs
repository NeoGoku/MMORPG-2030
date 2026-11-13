using MMORPG.Server;
public static class Cycle2Tests
{
 public static void Run(){
  void Check(bool v,string label){if(!v)throw new Exception(label);Console.WriteLine("PASS cycle2: "+label);}
  var d=District.Load("Client/data/district.json");var g=GameCatalog.Load("Client/data/game.json");var store=new MemoryGameStore();var account=Guid.NewGuid().ToString("N");store.Register(new(account,account,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=account};store.Create(c);var w=new World(d,g,store);w.JoinCharacter(c);w.Input(c.Id,1,1,0);w.Step(.1);Check(store.Get(c.Id)!.State.X==0,"movement has not yet reached routine checkpoint");w.StopAsync(CancellationToken.None).GetAwaiter().GetResult();Check(store.Get(c.Id)!.State.X>0,"orderly stop persists last movement before checkpoint interval");
  var low=new World(d,g);var id=low.JoinCharacter(new CharacterRecord{State=new CharacterState{X=0,Z=-72,Level=1}})!;Check(low.View(id).Players.Single().Z==d.Spawn.Z,"reconnect cannot bypass region level requirement");
  var high=new World(d,g);var hid=high.JoinCharacter(new CharacterRecord{State=new CharacterState{X=0,Z=-72,Level=10}})!;Check(high.View(hid).Players.Single().Z==-72,"qualified character enters the same open dungeon region");
 }
}
