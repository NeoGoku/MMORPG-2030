using MMORPG.Server;
public static class InstanceEconomyTests
{
 public static void Run(){
  void Check(bool value,string label){if(!value)throw new Exception(label);Console.WriteLine("PASS instance economy: "+label);}
  var g=GameCatalog.Load("Client/data/game.json");var store=new MemoryGameStore();
  CharacterRecord Create(){var a=Guid.NewGuid().ToString("N");store.Register(new(a,a,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=a,State=new CharacterState{X=30,Z=12,Coins=100}};store.Create(c);return c;}
  var a=Create();var b=Create();var gear=Guid.NewGuid().ToString("N");a.State.Gear[gear]=new GearState{Item="iron_sword",Condition=17};var petId=Guid.NewGuid().ToString("N");a.State.OwnedPets.Add(new PetState{Id=petId,Level=3});store.Save(a);
  var world=new World(District.Load("Client/data/district.json"),g,store);world.JoinCharacter(a);world.JoinCharacter(b);long sa=0,sb=0;
  world.Act(a.Id,++sa,"trade_open",b.Id);world.Act(a.Id,++sa,"trade_offer","",gear+"|1|0");world.Act(a.Id,++sa,"trade_confirm","");world.Act(b.Id,++sb,"trade_confirm","");
  Check(!store.Get(a.Id)!.State.Gear.ContainsKey(gear)&&store.Get(b.Id)!.State.Gear[gear].Condition==17,"direct trade preserves gear identity and condition");
  world.Act(a.Id,++sa,"market_pet",petId,"10");var listing=store.LoadWorld().Auctions.Single().Key;world.Act(b.Id,++sb,"market_buy",listing);world.Act(b.Id,++sb,"market_buy",listing);
  Check(store.Get(b.Id)!.State.OwnedPets.Count(p=>p.Id==petId)==1&&!store.Get(a.Id)!.State.OwnedPets.Any(p=>p.Id==petId),"pet escrow preserves identity without repeated purchase");world.Leave(a.Id);world.Leave(b.Id);
 }
}
