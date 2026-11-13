namespace MMORPG.Server;
// Rebuilt once after a world mutation, reused by every recipient snapshot in that tick.
public sealed class SpatialIndex
{
 private readonly Dictionary<(int,int),List<string>> cells=[];
 public const int CellSize=24;
 public void Rebuild(IEnumerable<(string Id,double X,double Z)> entities){cells.Clear();foreach(var e in entities){var key=((int)Math.Floor(e.X/CellSize),(int)Math.Floor(e.Z/CellSize));if(!cells.TryGetValue(key,out var bucket))cells[key]=bucket=[];bucket.Add(e.Id);}}
 public IEnumerable<string> Query(double x,double z,double radius){for(var cx=(int)Math.Floor((x-radius)/CellSize);cx<=(int)Math.Floor((x+radius)/CellSize);cx++)for(var cz=(int)Math.Floor((z-radius)/CellSize);cz<=(int)Math.Floor((z+radius)/CellSize);cz++)if(cells.TryGetValue((cx,cz),out var bucket))foreach(var id in bucket)yield return id;}
}
