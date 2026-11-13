namespace MMORPG.Server;

public sealed partial class World
{
    private double Elevation(Player p)=>p.Data.State.Floor*Catalog.HousingFloorHeight;
    private static bool SameLayer(Player a,Player b)=>a.Data.State.Floor==b.Data.State.Floor&&(a.Data.State.Floor==0||a.Data.State.HomeId==b.Data.State.HomeId);
    private static void ResetInterior(Player p){p.Data.State.Floor=0;p.Data.State.HomeId="";}
}
