namespace MMORPG.Server;

public sealed partial class World(District district, GameCatalog? game = null, IGameStore? persistence = null) : BackgroundService
{
    private readonly object gate = new();
    private readonly Dictionary<string, Player> players = new();
    private readonly Dictionary<string, Monster> monsters = district.Monsters.Concat(district.Encounters).ToDictionary(s=>s.Id,s=>new Monster(s,s.Hp>0?s.Hp:district.Rules.MonsterHp));
    private readonly Dictionary<string, Drop> loot = new();
    private double time;
    private long tick;
    private Rules R => district.Rules;
    private sealed class Player(string id, double x, double z, int hp)
    {
        public string Id = id; public double X=x, Z=z, Yaw, Dx, Dz, LastInput=-10, NextAttack, Respawn;
        public int Hp=hp, Level=1, Exp, StatPoints, Coins, Scrap, Kills, AttackSerial, HurtSerial, LastDamage;
        public long Sequence=-1;
        public string MarketSearch="";
        public string Target="", Quest="available", Message="Sprich mit Mira bei der Stadtwache.";
        public bool Attacking;
        public CharacterRecord Data = new();
        public int Mana=100, SkillSerial; public double BuffUntil, NextPet;
        public Dictionary<string,double> Cooldowns=[];
        public double PetX,PetZ;public FarmState Farm=new();
    }
    private sealed class Monster(Spawn spawn, int hp)
    {
        public Spawn Spawn=spawn;
        public double WarningUntil,WarningX,WarningZ,NextWave;public Dictionary<string,double> Contributors=[];
        public double X=spawn.X,Z=spawn.Z, NextAttack, Respawn;
        public int Hp=hp, AttackSerial, HurtSerial, LastDamage;
        public string DotOwner="",ForcedTarget="";public double DotUntil,NextDot,SlowUntil,BreakUntil,TauntUntil,HardTauntUntil;public int DotPower;
    }
    private sealed record Drop(string Id, string Owner, string MonsterId, double X, double Z, double Expires);

    public string? Join()
    {
        lock(gate) {
            if(players.Count>=100)return null;
            var id=Guid.NewGuid().ToString("N");
            // Small deterministic offset keeps simultaneous arrivals distinguishable.
            players[id]=new Player(id,district.Spawn.X+(players.Count%5-2)*.8,district.Spawn.Z,R.PlayerHp);
            players[id].Data.Id=id;players[id].Data.State.Coins=0;
            return id;
        }
    }
    public void Leave(string id) { lock(gate) {
        if(players.TryGetValue(id,out var p)&&p.Data.AccountId!="") {
            try { Store.Save(Export(p)); } catch(Exception e) when(e is GameRuleException or Npgsql.NpgsqlException) { pendingSaves[id]=CharacterRules.Clone(Export(p)); }
        }
        SocialDisconnect(id);summons.Remove(id);players.Remove(id);foreach(var d in loot.Values.Where(l=>l.Owner==id).ToArray())loot.Remove(d.Id);
    } }
    private bool Accept(string id,long seq,out Player p) { return players.TryGetValue(id,out p!)&&seq>=0&&seq>p.Sequence&&(p.Sequence=seq)>=0; }
    public void Input(string id,long seq,double x,double z)
    {
        lock(gate) {
            if(!double.IsFinite(x)||!double.IsFinite(z)||Math.Abs(x)>1000||Math.Abs(z)>1000||!Accept(id,seq,out var p))return;
            if(p.Farm.Active&&Math.Abs(x)+Math.Abs(z)>.01)StopFarm(p);
            var scale=Math.Max(1,Math.Sqrt(x*x+z*z));p.Dx=x/scale;p.Dz=z/scale;p.LastInput=time;
        }
    }
    public void Act(string id,long seq,string kind,string target,string text="")
    {
        lock(gate) {
            if(!Accept(id,seq,out var p)||p.Hp<=0)return;
            if(ArenaAction(p,kind,target))return;
            if(CommunityAction(p,kind,target,text))return;
            if(SocialAction(p,kind,target,text)||TradeAction(p,kind,target,text))return;
            var backup=CharacterRules.Clone(Export(p));var oldHp=p.Hp;var oldPeers=players.Values.Where(v=>v.Id!=p.Id&&PartyOf(v.Id)?.Id==PartyOf(p.Id)?.Id&&PartyOf(p.Id) is not null).ToDictionary(v=>v.Id,v=>CharacterRules.Clone(Export(v)));var oldLoot=new Dictionary<string,Drop>(loot);
            var oldMonsters=monsters.ToDictionary(x=>x.Key,x=>CloneMonster(x.Value));var oldSummons=CloneSummons();var oldFarm=p.Farm.Copy();
            if(p.Farm.Active&&(kind is "target" or "attack" or "skill" or "teleport"))StopFarm(p);
            if(ExtendedAction(p,kind,target)) { CommitAction(p,backup,oldHp,oldLoot,oldMonsters,oldPeers,oldSummons,oldFarm);return; }
            switch(kind) {
                case "target":
                    p.Target=monsters.TryGetValue(target,out var m)&&m.Hp>0&&Distance(p.X,p.Z,m.X,m.Z)<=24?target:"";
                    p.Attacking=false;break;
                case "attack":
                    p.Attacking=!p.Attacking&&p.Target!="";p.Message=p.Attacking?"Auto-Angriff aktiv. Stehen bleiben und in Reichweite gehen.":"Auto-Angriff beendet.";break;
                case "interact":
                    if(Distance(p.X,p.Z,district.Npc.X,district.Npc.Z)>3){p.Message="Mira ist zu weit entfernt.";break;}
                    if(p.Quest=="available"){p.Quest="active";p.Message="Mira: Zerstöre 3 defekte Drohnen im Nordhof. Komm danach zurück.";}
                    else if(p.Quest=="ready"){p.Quest="complete";p.Coins+=R.QuestCoins;GrantExp(p,R.QuestExp);p.Hp=CharacterRules.MaxHp(p.Data,Catalog);p.Message="Mira: Nordtor ist sicherer. Danke! +60 EXP, +25 Münzen.";}
                    else p.Message=p.Quest=="complete"?"Mira: Gute Arbeit. Die Wache hält hier die Stellung.":$"Mira: Noch {Math.Max(0,R.QuestKills-p.Kills)} Drohnen im Nordhof.";
                    break;
                case "loot":
                    var drop=target!=""?loot.GetValueOrDefault(target):loot.Values.Where(l=>l.Owner==id&&Distance(p.X,p.Z,l.X,l.Z)<=3).OrderBy(l=>Distance(p.X,p.Z,l.X,l.Z)).FirstOrDefault();
                    if(drop is not null&&drop.Owner==id&&Distance(p.X,p.Z,drop.X,drop.Z)<=3&&CharacterRules.Add(p.Data.State,DropItem(drop),1,Catalog)){loot.Remove(drop.Id);p.Coins+=R.LootCoins;p.Scrap+=R.LootScrap;p.Message=$"Beute: +{R.LootCoins} Münzen, +{R.LootScrap} Drohnenschrott.";}
                    else p.Message="Keine eigene Beute in Reichweite.";
                    break;
            }
            CommitAction(p,backup,oldHp,oldLoot,oldMonsters,oldPeers,oldSummons,oldFarm);
        }
    }
    public void Step(double dt)
    {
        lock(gate) {
            time+=dt;tick++;
            foreach(var p in players.Values) {
                if(p.Hp<=0){StopFarm(p);if(time>=p.Respawn){p.Hp=CharacterRules.MaxHp(p.Data,Catalog);p.X=district.Spawn.X;p.Z=district.Spawn.Z;p.Message="An der Stadtwache wiederbelebt.";}continue;}
                FarmTick(p,dt);
                var moving=time-p.LastInput<=.25&&(Math.Abs(p.Dx)+Math.Abs(p.Dz)>.01);
                if(moving){Move(p,p.Dx*R.Speed*dt,p.Dz*R.Speed*dt);p.Yaw=Math.Atan2(-p.Dx,-p.Dz);}
                if(p.Attacking&&monsters.TryGetValue(p.Target,out var m)&&m.Hp>0) {
                    var range=Distance(p.X,p.Z,m.X,m.Z);
                    if(!moving&&range<=R.AttackRange&&time>=p.NextAttack) {
                        p.Yaw=Math.Atan2(p.X-m.X,p.Z-m.Z);p.NextAttack=time+R.AttackSeconds/(p.Cooldowns.GetValueOrDefault("status:haste")>time?1.4:1);p.AttackSerial++;
                        HitMonster(p,m,Damage(p));
                    }
                } else if(!monsters.TryGetValue(p.Target,out var selected)||selected.Hp<=0) {p.Attacking=false;p.Target="";}
                if(p.Z>2&&!monsters.Values.Any(m=>m.Hp>0&&Distance(p.X,p.Z,m.X,m.Z)<8)&&tick%20==0)p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+5);
                if(tick%20==0)p.Mana=Math.Min(Catalog.BaseMana+p.Data.State.Attributes.GetValueOrDefault("wisdom")*Catalog.WisdomMana,p.Mana+Catalog.ManaRegen);
                TickPet(p,dt);
            }
            EffectsTick(dt);
            foreach(var m in monsters.Values) {
                if(m.Hp<=0){if(time>=m.Respawn){m.Hp=MaxMonsterHp(m);m.X=m.Spawn.X;m.Z=m.Spawn.Z;}continue;}
                BossTick(m);
                var victim=MonsterTarget(m);
                if(victim is not null) {
                    var dist=Distance(victim.X,victim.Z,m.X,m.Z);
                    if(dist>m.Spawn.Reach){Move(m,(victim.X-m.X)/dist*m.Spawn.Speed*dt*(m.SlowUntil>time?.5:1),(victim.Z-m.Z)/dist*m.Spawn.Speed*dt*(m.SlowUntil>time?.5:1));}
                    else if(time>=m.NextAttack){m.NextAttack=time+R.MonsterAttackSeconds;m.AttackSerial++;var dealt=Math.Max(1,(R.MonsterDamage+m.Spawn.Level-1)-CharacterRules.Defense(victim.Data,Catalog)-(m.Spawn.DamageType=="arcane"?victim.Data.State.Attributes.GetValueOrDefault("resistance"):0)-(victim.BuffUntil>time?Catalog.BuffDefense:0));if(!AbsorbSummonHit(victim,dealt)){victim.Hp=Math.Max(0,victim.Hp-dealt);victim.HurtSerial++;victim.LastDamage=dealt;if(m.Spawn.DamageType=="arcane")victim.Cooldowns["status:weaken"]=time+4;}
                        if(victim.Hp==0){victim.Attacking=false;victim.Target="";victim.Dx=victim.Dz=0;victim.Respawn=time+R.PlayerRespawnSeconds;victim.Message="Besiegt. Wiederbelebung in 4 Sekunden. Kein EXP-Abzug in diesem Bezirk.";}}
                } else {
                    var patrolX=m.Spawn.X+Math.Sin(time*.5+Array.FindIndex(district.Monsters,s=>s.Id==m.Spawn.Id))*1.3;
                    var dist=Distance(m.X,m.Z,patrolX,m.Spawn.Z);
                    if(dist>.1)Move(m,(patrolX-m.X)/dist*dt,(m.Spawn.Z-m.Z)/dist*dt);
                    if(Distance(m.X,m.Z,m.Spawn.X,m.Spawn.Z)<2)m.Hp=MaxMonsterHp(m);
                }
            }
            foreach(var d in loot.Values.Where(l=>l.Expires<time).ToArray())loot.Remove(d.Id);
            if(tick%20==0)foreach(var visitor in players.Values)if(!HomeAccess(visitor,visitor.X,visitor.Z)){visitor.X=district.Spawn.X;visitor.Z=district.Spawn.Z;visitor.Dx=visitor.Dz=0;}
            ArenaTick();
            SocialTick();
            if(tick%100==0)Checkpoint();
        }
    }
    private void Move(Player p,double dx,double dz){if(CanWalk(p,p.X+dx,p.Z))p.X+=dx;if(CanWalk(p,p.X,p.Z+dz))p.Z+=dz;}
    private void Move(Monster p,double dx,double dz){if(district.Walkable(p.X+dx,p.Z))p.X+=dx;if(district.Walkable(p.X,p.Z+dz))p.Z+=dz;}
    private static double Distance(double ax,double az,double bx,double bz)=>Math.Sqrt((ax-bx)*(ax-bx)+(az-bz)*(az-bz));
    private void GrantExp(Player p,int amount){p.Exp+=amount;while(p.Level<100&&p.Exp>=p.Level*Catalog.LevelExpBase){p.Exp-=p.Level*Catalog.LevelExpBase;p.Level++;p.StatPoints++;}}
    public WorldView View(string id)
    {
        lock(gate) {
            var p=players[id];
            return new("snapshot",tick,players.Values.Where(v=>v.Id==id||Distance(v.X,v.Z,p.X,p.Z)<=Catalog.InterestRadius).Select(v=>new PlayerView(v.Id,v.X,v.Z,v.Yaw,VisibleHp(v),VisibleMaxHp(v),v.Level,v.AttackSerial,v.HurtSerial,v.LastDamage,v.Target,v.Attacking,v.Hp<=0,v.Data.Name,v.Data.ClassId,v.SkillSerial)).ToArray(),
                monsters.Values.Where(m=>Distance(m.X,m.Z,p.X,p.Z)<=Catalog.InterestRadius).Select(m=>new MonsterView(m.Spawn.Id,m.Spawn.Name,m.X,m.Z,m.Hp,MaxMonsterHp(m),m.AttackSerial,m.HurtSerial,m.LastDamage,m.Hp<=0,m.Spawn.Level,m.Spawn.Boss,Math.Max(0,m.WarningUntil-time),m.WarningX,m.WarningZ)).ToArray(),
                loot.Values.Where(l=>l.Owner==id&&Distance(l.X,l.Z,p.X,p.Z)<=Catalog.InterestRadius).Select(l=>new LootView(l.Id,l.MonsterId,l.X,l.Z)).ToArray(),
                new(p.Quest,p.Kills,p.Exp,p.Level,p.StatPoints,p.Coins,p.Scrap,p.Message,CharacterView(p)),
                players.Values.Where(v=>v.Data.State.Pet.Active&&(v.Id==id||Distance(v.PetX,v.PetZ,p.X,p.Z)<=Catalog.InterestRadius)).Select(v=>new PetView(v.Id,v.PetX,v.PetZ,v.Data.State.Pet.Level,v.Data.State.Pet.Mode,v.Data.State.Pet.Hp,v.Data.State.Pet.Evolution)).ToArray(),SocialView(p),SummonViews(p),new {phase=(time%Catalog.DaySeconds)/Catalog.DaySeconds,weather=new[]{"klar","Regen","Nebel"}[(int)(time/Catalog.WeatherSeconds)%3]},CommunityView(p),ArenaView(p));
        }
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken){using var timer=new PeriodicTimer(TimeSpan.FromMilliseconds(50));while(await timer.WaitForNextTickAsync(stoppingToken))Step(.05);}
}
public sealed record PlayerView(string Id,double X,double Z,double Yaw,int Hp,int MaxHp,int Level,int AttackSerial,int HurtSerial,int LastDamage,string Target,bool Attacking,bool Dead,string Name="Reisender",string ClassId="Shield Sword",int SkillSerial=0);
public sealed record MonsterView(string Id,string Name,double X,double Z,int Hp,int MaxHp,int AttackSerial,int HurtSerial,int LastDamage,bool Dead,int Level=1,bool Boss=false,double Warning=0,double WarningX=0,double WarningZ=0);
public sealed record LootView(string Id,string MonsterId,double X,double Z);
public sealed record SelfView(string Quest,int Kills,int Exp,int Level,int StatPoints,int Coins,int Scrap,string Message,object? Character=null);
public sealed record WorldView(string Type,long Tick,PlayerView[] Players,MonsterView[] Monsters,LootView[] Loot,SelfView Self,PetView[] Pets,object? Social=null,SummonView[]? Summons=null,object? Environment=null,object? Community=null,object? Arena=null);
public sealed record PetView(string Owner,double X,double Z,int Level,string Mode,int Hp=100,int Evolution=0);
