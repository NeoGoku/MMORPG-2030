using MMORPG.Server;

var district = District.Load(args.Length > 0 ? args[0] : "Client/data/district.json");
var world = new World(district);
var a = world.Join()!;
var b = world.Join()!;
long seq = 0;
void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS: " + label); }
void Step(int ticks) { for (var i=0; i<ticks; i++) world.Step(.05); }
void Move(double x, double z, int ticks) { for(var i=0;i<ticks;i++){world.Input(a, ++seq, x, z);world.Step(.05);} world.Input(a,++seq,0,0); }
PlayerView Player() => world.View(a).Players.Single(p=>p.Id==a);
void Go(double x, double z) { for(var i=0;i<600;i++){var p=Player();var dx=x-p.X;var dz=z-p.Z;if(Math.Sqrt(dx*dx+dz*dz)<.15)break; Move(dx,dz,1);} }
world.Act(a,++seq,"interact","");
Check(world.View(a).Self.Quest=="available", "NPC interaction requires range");
Go(-3,7);
world.Act(a,++seq,"interact","");
Check(world.View(a).Self.Quest=="active", "nearby NPC offers hunt");
world.Act(a,++seq,"target","drone-1");
Step(5);
Check(Player().Target=="drone-1", "idle target survives multiple ticks before attack tap");
world.Act(a,++seq,"attack","");
Step(40);
Check(world.View(a).Monsters.All(m=>m.Hp==42), "out-of-range auto attack cannot damage");
// Server movement cannot cross building footprints or teleport from input magnitude.
Go(0,10);Move(-100,0,100);
Check(Player().X > -11.5, "buildings block authoritative movement");
Go(0,3);
foreach(var id in new[]{"drone-1","drone-2","drone-3"}) {
 var m=world.View(a).Monsters.Single(m=>m.Id==id);
 Go(m.X,m.Z+2.6);
 world.Act(a,++seq,"target",id);Step(3);world.Act(a,++seq,"attack","");
 var hpBeforeMovement=world.View(a).Monsters.Single(m=>m.Id==id).Hp;
 Move(0,.1,8);
 Check(world.View(a).Monsters.Single(m=>m.Id==id).Hp==hpBeforeMovement,"normal attacks stop while moving");
 Step(70);
 Check(world.View(a).Monsters.Single(m=>m.Id==id).Dead, "stationary auto attack kills "+id);
 var loot=world.View(a).Loot.Single(l=>l.MonsterId==id);
 world.Act(b,1+seq,"loot",loot.Id);
 Check(world.View(b).Self.Coins==0,"loot is personal");
 Go(loot.X,loot.Z);
 world.Act(a,++seq,"loot",loot.Id);var coins=world.View(a).Self.Coins;
 world.Act(a,++seq,"loot",loot.Id);
 Check(world.View(a).Self.Coins==coins,"loot cannot be collected twice");
}
Check(world.View(a).Self.Quest=="ready"&&world.View(a).Self.Kills==3,"three kills complete objective");
Go(0,3);Go(-3,6);
world.Act(a,++seq,"interact","");
var reward=world.View(a).Self;
Check(reward.Quest=="complete"&&reward.Level==2&&reward.StatPoints==1&&reward.Exp==20&&reward.Coins==37&&reward.Scrap==3,"quest reward, loot and level progression");
world.Act(a,++seq,"interact","");
Check(world.View(a).Self.Coins==reward.Coins,"quest reward cannot be repeated");
world.Input(a,seq-10,1,0);var before=Player();Step(10);
Check(Player().X==before.X&&Player().Z==before.Z,"replayed input ignored");
var deathWorld=new World(district);var doomed=deathWorld.Join()!;long ds=0;
for(var i=0;i<80;i++){deathWorld.Input(doomed,++ds,0,-1);deathWorld.Step(.05);}
var witnessedDeath=false;var witnessedDamage=false;
for(var i=0;i<600;i++){deathWorld.Step(.05);var victim=deathWorld.View(doomed).Players.Single();witnessedDeath|=victim.Dead;witnessedDamage|=victim.HurtSerial>0;}
Check(witnessedDeath&&witnessedDamage,"monster attacks actually damage and kill player");
Check(deathWorld.View(doomed).Players.Single().Hp>0&&deathWorld.View(doomed).Players.Single().Z>0,"monster damage, player death and safe respawn");
world.Leave(a);Check(world.View(b).Players.Length==1,"disconnect removes visible player");
Console.WriteLine("All authoritative gameplay scenarios passed.");

M3Tests.Run();
M3Regression.Run();
M4Tests.Run();
M4PetTests.Run();
M4ReviewTests.Run();

M5Tests.Run();
M5ArenaTests.Run();
M5WorldTests.Run();
M5ReviewTests.Run();
