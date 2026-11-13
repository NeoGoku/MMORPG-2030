namespace MMORPG.Server;

public sealed partial class World
{
    private bool WorldTransit(Player p,string id)
    {
        var passage=district.Passages.FirstOrDefault(d=>d.Id==id);
        if(passage is null)return false;
        Point destination;
        if(Distance(p.X,p.Z,passage.From.X,passage.From.Z)<=3){
            if(p.Level<passage.MinLevel)return false;
            destination=passage.To;
        }else if(Distance(p.X,p.Z,passage.To.X,passage.To.Z)<=3)destination=passage.From;
        else return false;
        if(!CanWalk(p,destination.X,destination.Z))return false;
        StopFarm(p);p.X=destination.X;p.Z=destination.Z;p.Dx=p.Dz=0;
        p.Target="";p.Attacking=false;return true;
    }
}
