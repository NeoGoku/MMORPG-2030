using MMORPG.Server;
public static class CheckpointTests
{
 private sealed class Store:IGameStore
 {
  public MemoryGameStore Inner=new();public bool Fail=true;public bool FailSingles;public int LastBatch;
  public bool Durable=>false;public AccountRecord? Account(string s)=>Inner.Account(s);public void Register(AccountRecord a)=>Inner.Register(a);public List<CharacterRecord> List(string a)=>Inner.List(a);public CharacterRecord? Get(string id)=>Inner.Get(id);public void Create(CharacterRecord c)=>Inner.Create(c);public void Save(CharacterRecord c){if(FailSingles)throw new GameRuleException("test disconnect outage");Inner.Save(c);}
  public void SaveBatch(IReadOnlyList<CharacterRecord> batch){LastBatch=batch.Count;if(Fail)throw new GameRuleException("test conflict");Inner.SaveBatch(batch);}
 }
 public static void Run(){var s=new Store();CharacterRecord Make(){var a=Guid.NewGuid().ToString("N");s.Register(new(a,a,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=a};s.Create(c);return c;}var a=Make();var b=Make();var world=new World(District.Load("Client/data/district.json"),GameCatalog.Load("Client/data/game.json"),s);world.JoinCharacter(a);world.JoinCharacter(b);world.Act(a.Id,1,"party_invite",b.Id);world.Act(b.Id,1,"party_accept",a.Id);world.StopAsync(CancellationToken.None).GetAwaiter().GetResult();if(s.LastBatch!=2||s.Get(a.Id)!.Version!=0||s.Get(b.Id)!.Version!=0)throw new Exception("Party checkpoint partially committed");s.Fail=false;world.StopAsync(CancellationToken.None).GetAwaiter().GetResult();if(s.Get(a.Id)!.Version!=1||s.Get(b.Id)!.Version!=1)throw new Exception("Forced shutdown retry did not commit party together");Console.WriteLine("PASS checkpoint: party save atomic and forced retry preserves both revisions");
  world.Input(a.Id,2,1,0);world.Step(.1);s.FailSingles=true;world.Leave(a.Id);
  if(world.JoinCharacter(s.Get(a.Id)!) is not null)throw new Exception("Reconnected from stale SQL state during pending save");
  s.FailSingles=false;if(world.JoinCharacter(s.Get(a.Id)!)!=a.Id||s.Get(a.Id)!.State.X<=0)throw new Exception("Reconnect did not flush pending state after recovery");
  Console.WriteLine("PASS checkpoint: reconnect flushes pending disconnect state and refuses stale reconnect during outage");}
}
