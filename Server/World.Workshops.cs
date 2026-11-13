using System.Numerics;
namespace MMORPG.Server;
public sealed partial class World
{
    private ProductionJob BeginWork(CharacterState state,string recipeId)
    {
        Need(Catalog.Recipes.TryGetValue(recipeId,out var recipe),"Rezept unbekannt.");var r=recipe!;
        Need(1+state.Professions.GetValueOrDefault(r.Profession)/Catalog.ProfessionLevelXp>=r.MinProfessionLevel&&(r.Tool==""||state.Inventory.GetValueOrDefault(r.Tool)>0),"Berufsstufe oder Werkzeug fehlt.");
        foreach(var material in r.Inputs)Take(state,material.Key,material.Value);
        return new(){Floor=state.Floor,Inputs=new(r.Inputs),Recipe=recipeId,Item=r.Output,Count=r.Count,Profession=r.Profession,ReadyAt=Utc+Catalog.ProductionSeconds};
    }
    private void FinishWork(CharacterState state,ProductionJob job)
    {
        Need(job.ReadyAt<=Utc,"Auftrag noch nicht fertig.");
        Need((long)state.Professions.GetValueOrDefault(job.Profession)+Catalog.ProfessionCraftXp<=int.MaxValue,"Berufs-EXP-Limit erreicht.");
        Give(state,job.Item,job.Count);state.Professions[job.Profession]=state.Professions.GetValueOrDefault(job.Profession)+Catalog.ProfessionCraftXp;
        state.FarmXp=(BigInteger.Parse(state.FarmXp)+25).ToString();CharacterRules.Progress(state,"craft",job.Recipe,Catalog);
    }
    private void RefundWork(CharacterState state,ProductionJob job)
    {
        Need(job.Inputs.Count>0,"Alter Auftrag ohne erstattbare Materialliste: fertigstellen und abholen.");
        foreach(var material in job.Inputs)Give(state,material.Key,material.Value);
    }
    private bool StationBusy(HomeState home,string id)=>home.WorkOrders.ContainsKey(id)||(home.Production is {} legacy&&home.Furniture.TryGetValue(id,out var f)&&f.Floor==legacy.Floor&&f.Item=="oak_table");
    private string WorkshopAction(HomeState home,CharacterState state,string kind,string text)
    {
        var parts=text.Split('|');var stationId=parts[0];
        Need(home.Furniture.TryGetValue(stationId,out var station)&&station.Floor==state.Floor&&Distance(state.X,state.Z,station.X,station.Z)<=3,"Eigene Produktionsanlage auf diesem Geschoss besuchen (3 m).");
        if(kind=="home_work_start"){
            Need(parts.Length==2&&!StationBusy(home,stationId)&&home.WorkOrders.Count+(home.Production is null?0:1)<Catalog.HousingMaxWorkOrders,"Station belegt oder Auftragslimit erreicht.");
            Need(Catalog.Items.TryGetValue(station!.Item,out var item)&&Catalog.WorkstationProfessions.TryGetValue(item.FurnitureKind,out var professions)&&Catalog.Recipes.TryGetValue(parts[1],out var recipe)&&professions.Contains(recipe.Profession),"Rezept passt nicht zu dieser Anlage.");
            home.WorkOrders[stationId]=BeginWork(state,parts[1]);return "Produktionsauftrag an Anlage gebunden; läuft auch offline.";
        }
        Need(home.WorkOrders.TryGetValue(stationId,out var job)&&job.Floor==state.Floor,"Kein eigener Auftrag an dieser Anlage.");
        if(kind=="home_work_collect")FinishWork(state,job!);
        else if(kind=="home_work_cancel")RefundWork(state,job!);
        else throw new GameRuleException("Unbekannte Werkstattaktion.");
        home.WorkOrders.Remove(stationId);return kind=="home_work_collect"?"Erzeugnis und Berufsfortschritt einmalig abgeholt.":"Originalmaterialien einmalig erstattet.";
    }
}
