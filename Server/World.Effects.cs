namespace MMORPG.Server;

public sealed partial class World
{
    private sealed class Summon(string owner,string kind,double x,double z,double until)
    {
        public string Owner=owner,Kind=kind;
        public double X=x,Z=z,Until=until,NextAttack;
        public int Hp=60;
    }
    private readonly Dictionary<string,Summon> summons=[];
    private Monster CloneMonster(Monster m)=>new(m.Spawn,m.Hp){X=m.X,Z=m.Z,WarningUntil=m.WarningUntil,WarningX=m.WarningX,WarningZ=m.WarningZ,NextWave=m.NextWave,Threat=new(m.Threat),Contributors=new(m.Contributors),NextAttack=m.NextAttack,Respawn=m.Respawn,AttackSerial=m.AttackSerial,HurtSerial=m.HurtSerial,LastDamage=m.LastDamage,DotOwner=m.DotOwner,DotUntil=m.DotUntil,NextDot=m.NextDot,DotPower=m.DotPower,SlowUntil=m.SlowUntil,BreakUntil=m.BreakUntil,ForcedTarget=m.ForcedTarget,TauntUntil=m.TauntUntil,HardTauntUntil=m.HardTauntUntil};
    private Dictionary<string,Summon> CloneSummons()=>summons.ToDictionary(s=>s.Key,s=>new Summon(s.Value.Owner,s.Value.Kind,s.Value.X,s.Value.Z,s.Value.Until){NextAttack=s.Value.NextAttack,Hp=s.Value.Hp});
    private void HitMonster(Player p,Monster m,int raw)
    {
        if(m.Hp<=0)return;
        m.Contributors[p.Id]=time;foreach(var id in m.Contributors.Where(c=>time-c.Value>4).Select(c=>c.Key).ToArray())m.Contributors.Remove(id);
        if(m.Spawn.Boss&&m.Contributors.Count<2)raw=Math.Max(1,raw/2);
        var amount=Math.Max(1,raw+(m.BreakUntil>time?Catalog.StatusPenalty:0));
        m.Hp=Math.Max(0,m.Hp-amount);m.HurtSerial++;m.LastDamage=amount;
        m.Threat[p.Id]=Math.Min(1000000,m.Threat.GetValueOrDefault(p.Id)+amount);
        if(m.Hp==0)RewardKill(p,m);
    }
    private int MaxMonsterHp(Monster m)=>m.Spawn.Hp>0?m.Spawn.Hp:R.MonsterHp;
    private void BossTick(Monster m)
    {
        if(!m.Spawn.Boss)return;
        var target=MonsterTarget(m);
        if(target is null){m.WarningUntil=0;return;}
        if(m.WarningUntil>0&&time>=m.WarningUntil){
            var victims=players.Values.Where(p=>p.Hp>0&&Distance(p.X,p.Z,m.WarningX,m.WarningZ)<5).ToArray();
            foreach(var p in victims){var damage=Math.Max(1,30/Math.Max(1,victims.Length));p.Hp=Math.Max(0,p.Hp-damage);p.HurtSerial++;p.LastDamage=damage;if(p.Hp==0){p.Respawn=time+R.PlayerRespawnSeconds;p.Attacking=false;p.Target="";p.Dx=p.Dz=0;}}
            m.WarningUntil=0;m.NextWave=time+6;
        }else if(m.WarningUntil==0&&time>=m.NextWave){m.WarningX=target.X;m.WarningZ=target.Z;m.WarningUntil=time+2.5;}
    }
    private IEnumerable<Player> FriendlyTargets(Player caster,double radius)
    {
        var group=PartyOf(caster.Id);
        return players.Values.Where(p=>p.Hp>0&&(p.Id==caster.Id||(group is not null&&group.Members.ContainsKey(p.Id)&&Distance(caster.X,caster.Z,p.X,p.Z)<=radius)));
    }
    private Player? MonsterTarget(Monster m)
    {
        if(m.TauntUntil>time&&players.TryGetValue(m.ForcedTarget,out var forced)&&forced.Hp>0&&Distance(forced.X,forced.Z,m.Spawn.X,m.Spawn.Z)<18)return forced;
        return players.Values.Where(p=>p.Hp>0&&Distance(p.X,p.Z,m.X,m.Z)<R.AggroRange&&Distance(p.X,p.Z,m.Spawn.X,m.Spawn.Z)<12).OrderByDescending(p=>m.Threat.GetValueOrDefault(p.Id)).ThenBy(p=>Distance(p.X,p.Z,m.X,m.Z)).FirstOrDefault();
    }
    private void EffectsTick(double dt)
    {
        foreach(var m in monsters.Values.ToArray())if(m.Hp>0&&m.DotUntil>time&&m.NextDot<=time&&players.TryGetValue(m.DotOwner,out var owner)){
            m.NextDot=time+1;HitMonster(owner,m,m.DotPower);
        }
        foreach(var p in players.Values)if(p.Hp>0&&p.Cooldowns.GetValueOrDefault("status:hot")>time&&tick%20==0)p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+Catalog.HotHeal);
        foreach(var pair in summons.ToArray()){
            var s=pair.Value;
            if(s.Hp<=0||s.Until<=time||!players.TryGetValue(s.Owner,out var p)||p.Hp<=0){summons.Remove(pair.Key);continue;}
            var tx=p.X-1;var tz=p.Z+1;
            if(p.Target!=""&&monsters.TryGetValue(p.Target,out var m)&&m.Hp>0&&Distance(p.X,p.Z,m.X,m.Z)<=18){
                tx=m.X;tz=m.Z;
                if(time>=s.NextAttack&&Distance(s.X,s.Z,m.X,m.Z)<=4){
                    s.NextAttack=time+2;
                    if(s.Kind=="wisp")p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+8);
                    else {HitMonster(p,m,(s.Kind=="wolf"?9:5)+ClassRules.Attack(p.Data,Catalog));if(s.Kind=="golem"&&m.HardTauntUntil<=time){m.ForcedTarget=p.Id;m.TauntUntil=time+4;}}
                }
            }else if(s.Kind=="wisp"&&time>=s.NextAttack){s.NextAttack=time+3;p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+8);}
            var dist=Distance(s.X,s.Z,tx,tz);if(dist>20){s.X=p.X;s.Z=p.Z;}
            else if(dist>1){var dx=(tx-s.X)/dist*dt*5;var dz=(tz-s.Z)/dist*dt*5;if(district.Walkable(s.X+dx,s.Z))s.X+=dx;if(district.Walkable(s.X,s.Z+dz))s.Z+=dz;}
        }
    }
    private bool AbsorbSummonHit(Player p,int damage)
    {
        if(summons.TryGetValue(p.Id,out var s)&&s.Kind=="golem"&&s.Hp>0&&Distance(s.X,s.Z,p.X,p.Z)<6){s.Hp=Math.Max(0,s.Hp-damage);return true;}
        var pet=p.Data.State.Pet;
        if(pet.Active&&pet.Mode=="tank"&&pet.Hp>0&&Distance(p.PetX,p.PetZ,p.X,p.Z)<6){pet.Hp=Math.Max(0,pet.Hp-damage);if(pet.Hp==0)pet.RecoveryUntil=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+15;return true;}
        return false;
    }
    private object SkillView(Player p,SkillDef s)=>new{id=s.Id,name=s.Name,kind=s.Kind,enabled=s.Enabled&&p.Level>=s.MinLevel,minLevel=s.MinLevel,cost=s.Cost,cooldown=Math.Max(0,p.Cooldowns.GetValueOrDefault(s.Id)-time)};
    private SummonView[] SummonViews(Player viewer)=>summons.Values.Where(s=>Distance(viewer.X,viewer.Z,s.X,s.Z)<=Catalog.InterestRadius).Select(s=>new SummonView(s.Owner,s.Kind,s.X,s.Z,s.Hp,60,Math.Max(0,s.Until-time))).ToArray();
}
public sealed record SummonView(string Owner,string Kind,double X,double Z,int Hp,int MaxHp,double Remaining);
