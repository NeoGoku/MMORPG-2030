namespace MMORPG.Server;
public sealed partial class World
{
    private sealed class FarmState
    {
        public bool Active,Returning;public double X,Z,Radius,Until,RealUntil,Idle,Stuck,LastX,LastZ;
        public List<Point> Path=[];
        public FarmState Copy()=>new(){Active=Active,Returning=Returning,X=X,Z=Z,Radius=Radius,Until=Until,RealUntil=RealUntil,Idle=Idle,Stuck=Stuck,LastX=LastX,LastZ=LastZ,Path=Path.Select(p=>new Point{X=p.X,Z=p.Z}).ToList()};
    }
    private void StopFarm(Player p){p.Farm.Active=false;p.Farm.Returning=false;p.Dx=p.Dz=0;p.Attacking=false;}
    private bool FarmAction(Player p,string kind,string target)
    {
        if(kind=="farm_stop"){StopFarm(p);p.Message="Automatik beendet.";return true;}
        if(kind!="farm_start")return false;
        if(!int.TryParse(target,out var radius)||radius<1||radius>Catalog.FarmRadius||p.Cooldowns.GetValueOrDefault("farm:ready")>time||p.Hp<CharacterRules.MaxHp(p.Data,Catalog)*.6||p.Z<2||monsters.Values.Any(m=>Distance(p.X,p.Z,m.Spawn.X,m.Spawn.Z)<12)){
            p.Message="Start am sicheren Stadtrand: Abstand zu Gegnern, mindestens 60 % HP, Radius begrenzt und Start-Cooldown.";return true;
        }
        p.Farm=new FarmState{Active=true,X=p.X,Z=p.Z,Radius=radius,Until=time+Catalog.FarmSeconds,RealUntil=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+Catalog.FarmSeconds,LastX=p.X,LastZ=p.Z,Path=[new Point{X=p.X,Z=p.Z}]};
        p.Cooldowns["farm:ready"]=time+Catalog.FarmSeconds+30;p.Message="Bewachte Automatik gestartet. Maximal 60 Sekunden; Bewegung beendet sofort.";return true;
    }
    private void FarmTick(Player p,double dt)
    {
        var f=p.Farm;if(!f.Active)return;
        var maxHp=CharacterRules.MaxHp(p.Data,Catalog);
        if(p.Hp<maxHp*.55&&p.Cooldowns.GetValueOrDefault("potion")<=time&&p.Data.State.Inventory.GetValueOrDefault("potion")>0){
            p.Data.State.Inventory["potion"]--;if(p.Data.State.Inventory["potion"]==0)p.Data.State.Inventory.Remove("potion");p.Hp=Math.Min(maxHp,p.Hp+Catalog.Items["potion"].Heal);p.Cooldowns["potion"]=time+3;
        }
        if(DateTimeOffset.UtcNow.ToUnixTimeSeconds()>=f.RealUntil||time>=f.Until||p.Hp<maxHp*.35||p.Mana<5||CharacterRules.Slots(p.Data.State,Catalog)>=Catalog.InventorySlots||Distance(p.X,p.Z,f.X,f.Z)>f.Radius+1)f.Returning=true;
        p.Dx=p.Dz=0;p.LastInput=time;
        if(f.Returning){
            p.Attacking=false;p.Target="";
            while(f.Path.Count>0&&Distance(p.X,p.Z,f.Path[^1].X,f.Path[^1].Z)<.6)f.Path.RemoveAt(f.Path.Count-1);
            if(f.Path.Count==0){StopFarm(p);p.Message="Automatik beendet; zum Startpunkt zurückgekehrt.";return;}
            SteerFarm(p,f.Path[^1].X,f.Path[^1].Z,dt);return;
        }
        if(f.Path.Count<256&&(f.Path.Count==0||Distance(p.X,p.Z,f.Path[^1].X,f.Path[^1].Z)>1))f.Path.Add(new Point{X=p.X,Z=p.Z});
        var drop=loot.Values.Where(d=>d.Owner==p.Id&&Distance(d.X,d.Z,f.X,f.Z)<=f.Radius).OrderBy(d=>Distance(p.X,p.Z,d.X,d.Z)).FirstOrDefault();
        if(drop is not null){
            p.Attacking=false;
            if(Distance(p.X,p.Z,drop.X,drop.Z)<=3){if(CharacterRules.Add(p.Data.State,DropItem(drop),1,Catalog)){loot.Remove(drop.Id);p.Coins+=R.LootCoins;p.Scrap+=R.LootScrap;}else f.Returning=true;}
            else SteerFarm(p,drop.X,drop.Z,dt);
            return;
        }
        var target=monsters.Values.Where(m=>m.Hp>0&&Distance(m.X,m.Z,f.X,f.Z)<=f.Radius).OrderBy(m=>Distance(p.X,p.Z,m.X,m.Z)).FirstOrDefault();
        if(target is null){p.Attacking=false;f.Idle+=dt;if(f.Idle>15)f.Returning=true;return;}f.Idle=0;
        p.Target=target.Spawn.Id;
        if(Distance(p.X,p.Z,target.X,target.Z)>R.AttackRange){p.Attacking=false;SteerFarm(p,target.X,target.Z,dt);}
        else{
            p.Attacking=true;
            var skill=Catalog.Classes[p.Data.ClassId].Skills.FirstOrDefault(s=>s.Enabled&&s.MinLevel<=p.Level&&s.Kind=="damage"&&p.Mana>=s.Cost&&p.Cooldowns.GetValueOrDefault(s.Id)<=time);
            if(skill is not null)Cast(p,skill.Id);
        }
    }
    private void SteerFarm(Player p,double x,double z,double dt)
    {
        var f=p.Farm;var dx=x-p.X;var dz=z-p.Z;var distance=Math.Sqrt(dx*dx+dz*dz);if(distance<.1)return;
        if(Distance(p.X,p.Z,f.LastX,f.LastZ)<.01)f.Stuck+=dt;else f.Stuck=0;
        f.LastX=p.X;f.LastZ=p.Z;
        if(f.Stuck>3){if(f.Returning){StopFarm(p);p.Message="Automatik sicher gestoppt: Rückweg blockiert.";}else {f.Returning=true;f.Stuck=0;}return;}
        var angle=Math.Atan2(dz,dx);
        foreach(var turn in new[]{0.0,Math.PI/4,-Math.PI/4,Math.PI/2,-Math.PI/2}){
            var vx=Math.Cos(angle+turn);var vz=Math.Sin(angle+turn);
            if(district.Walkable(p.X+vx*R.Speed*dt,p.Z+vz*R.Speed*dt)){p.Dx=vx;p.Dz=vz;return;}
        }
    }
    private object FarmView(Player p)=>new{active=p.Farm.Active,returning=p.Farm.Returning,remaining=Math.Max(0,Math.Min(p.Farm.Until-time,p.Farm.RealUntil-DateTimeOffset.UtcNow.ToUnixTimeSeconds())),radius=p.Farm.Radius,homeX=p.Farm.X,homeZ=p.Farm.Z};
}
