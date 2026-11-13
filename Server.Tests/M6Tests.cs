using MMORPG.Server;
using System.Text.Json;
public static class M6Tests
{
 public static void Run(){
  void Check(bool ok,string s){if(!ok)throw new Exception(s);Console.WriteLine("PASS M6: "+s);}
  var g=GameCatalog.Load("Client/data/game.json");var d=District.Load("Client/data/district.json");var store=new MemoryGameStore();
  CharacterRecord Hero(){var id=Guid.NewGuid().ToString("N");store.Register(new(id,id,new byte[32],new byte[32]));var c=new CharacterRecord{AccountId=id};store.Create(c);return c;}
  var a=Hero();var b=Hero();var outsider=Hero();var w=store.LoadWorld();w.Guilds["guild"]=new GuildState{Leader=a.Id,Name="Test",Members=new(){{a.Id,"leader"},{b.Id,"member"}},Hall="guildhall-1"};store.SaveWorld(w,[]);
  var world=new World(d,g,store);world.JoinCharacter(a);world.JoinCharacter(b);world.JoinCharacter(outsider);world.Act(a.Id,1,"chat_guild","","Nur Gilde");
  var json=new JsonSerializerOptions(JsonSerializerDefaults.Web);JsonElement Social(string id)=>JsonSerializer.SerializeToElement(world.View(id).Social,json);
  Check(Social(b.Id).GetProperty("chat").GetArrayLength()==1&&Social(outsider.Id).GetProperty("chat").GetArrayLength()==0,"guild chat excludes outsiders");
  world.Act(b.Id,1,"guild_release","");Check(store.LoadWorld().Guilds["guild"].Hall!="","member cannot release guild hall");world.Act(a.Id,2,"guild_release","");Check(store.LoadWorld().Guilds["guild"].Hall=="","leader releases hall without destroying guild storage");
  var state=new CharacterState{Level=10,Inventory=new(){{"reset_stone",1}},Attributes=new(){{"strength",5},{"vitality",3}}};
  Check(CharacterRules.Reset(state,g)&&state.StatPoints==8&&state.Attributes.Values.Sum()==0&&!CharacterRules.Reset(state,g),"reset refunds invested points once and consumes rare item");
  state.Level=100;state.Inventory["boss_core"]=1;Check(CharacterRules.PrestigeUp(state,g)&&state.Prestige==1&&!CharacterRules.PrestigeUp(state,g),"prestige consumes core once at level cap");
  var daily=g.Quests.Single(q=>q.Id=="daily_herbs");var progress=new CharacterState{Level=2};for(var i=0;i<5;i++)CharacterRules.Progress(progress,"gather","herb",g);
  Check(CharacterRules.Claim(progress,daily.Id,g)&&progress.StatPoints==1&&!CharacterRules.Claim(progress,daily.Id,g),"daily quest grants stat point once per UTC day");
  progress.DailyClaims[daily.Id]=CharacterRules.Day-1;CharacterRules.Progress(progress,"gather","herb",g);Check(progress.QuestProgress[daily.Id]==1&&!CharacterRules.Claim(progress,daily.Id,g),"next day requires fresh objectives");
  Check(!CharacterRules.Claim(new CharacterState{QuestProgress=new(){{"reset_trial",3}}},"reset_trial",g),"quest claim validates level and prerequisite even with forged progress");
  foreach(var id in new[]{a.Id,b.Id,outsider.Id})world.Leave(id);
  var death=new World(d,g);var doomed=death.JoinCharacter(new CharacterRecord{State=new CharacterState{X=-7,Z=-8,Hp=1,Level=2,Exp=20}})!;death.Step(.05);death.Step(.05);death.Step(.05);Check(death.View(doomed).Self.Exp==19,"death EXP penalty applies exactly once during respawn wait");
  var rewards=GameCatalog.Load("Client/data/game.json");rewards.InventorySlots=0;var blocked=new CharacterState{Level=10,ClaimedQuests=new(){"core_guardian"},QuestProgress=new(){{"reset_trial",3}}};Check(!CharacterRules.Claim(blocked,"reset_trial",rewards)&&!blocked.Inventory.ContainsKey("reset_stone")&&!blocked.ClaimedQuests.Contains("reset_trial"),"full inventory rejects complete quest reward without partial claim");
  var market=store.LoadWorld();for(var i=0;i<41;i++)market.Auctions["offer"+i]=new AuctionState{Seller=b.Id,Item="potion",Count=1,Price=1,Created=DateTimeOffset.UtcNow.ToUnixTimeSeconds()};market.Auctions["expired"]=new AuctionState{Seller=b.Id,Item="potion",Count=1,Price=1,Created=1};store.SaveWorld(market,[]);
  var buyer=store.Get(a.Id)!;buyer.State.X=30;buyer.State.Z=12;store.Save(buyer);var exchange=new World(d,g,store);exchange.JoinCharacter(buyer);exchange.Act(a.Id,1,"market_page","1");var view=JsonSerializer.SerializeToElement(exchange.View(a.Id).Community,json);
  Check(view.GetProperty("marketPages").GetInt32()==3&&view.GetProperty("auctions").GetArrayLength()==20,"auction pagination exposes later offers");exchange.Act(a.Id,2,"market_buy","expired");Check(store.Get(a.Id)!.State.Coins==20&&store.LoadWorld().Auctions.ContainsKey("expired"),"expired escrow cannot be purchased or destroyed");exchange.Leave(a.Id);
  var seller=store.Get(b.Id)!;seller.State.X=30;seller.State.Z=12;store.Save(seller);var cancel=new World(d,g,store);cancel.JoinCharacter(seller);cancel.Act(b.Id,1,"market_cancel","expired");cancel.Act(b.Id,2,"market_cancel","expired");Check(store.Get(b.Id)!.State.Inventory["potion"]==3&&!store.LoadWorld().Auctions.ContainsKey("expired"),"expired items return once to their seller");cancel.Leave(b.Id);
 }
}
