using System.Globalization;
namespace MMORPG.Server;
public sealed partial class World
{
    private string HousingAction(SharedWorld w,CharacterRecord c,string kind,string target,string text)
    {
        var s=c.State;
        if(kind=="home_intro"){Need(Distance(s.X,s.Z,30,24)<=3,"Elin · Wohnamt steht bei (30,24).");s.HousingIntro=true;return "Elin: Sammle 3 Holz und baue einen Eichenstuhl. Wähle dann eine freie Wohnung am Wohnamt.";}
        var def=district.Homes.FirstOrDefault(d=>d.Id==target&&!d.GuildHall);Need(def is not null,"Wohnung auswählen.");
        if(kind=="home_claim"){
            Need(s.HousingIntro&&Distance(s.X,s.Z,30,24)<=3,"Erst Elins Wohnquest am Wohnamt annehmen.");
            Need(!w.Homes.ContainsKey(target)&&!w.Homes.Values.Any(h=>h.Owner==c.Id),"Wohnung belegt oder bereits Eigentümer.");
            Need(s.Inventory.GetValueOrDefault("oak_chair")>0,"Ein Eichenstuhl gehört zur Einführungsquest.");Take(s,"wood",3);
            w.Homes[target]=new HomeState{Owner=c.Id,RentUntil=Utc+604800};return "Wohnquest abgeschlossen. Wohnung erhalten; erste Mietwoche frei. Eingang links an der Südseite.";
        }
        Need(w.Homes.TryGetValue(target,out var home)&&home.Owner==c.Id,"Nur der Eigentümer darf diese Wohnung verwalten.");var h=home!;
        if(kind=="home_rent"){
            var periods=Math.Max(1,(Utc-h.RentUntil)/604800+1);Need(periods<=520,"Mietrückstand zu groß.");Coins(s,-checked((int)periods*10));h.RentUntil=Math.Max(Utc,h.RentUntil)+604800;return "Miete bezahlt. Möbel und Lager bleiben auch bei Rückstand erhalten.";
        }
        if(kind=="home_visit"){
            var pair=text.Split('|');Need(pair.Length==2&&pair[0].Length<=64,"Besucher-ID und allow/remove wählen.");
            if(pair[0]=="guild")h.GuildVisitors=pair[1]=="allow";
            else{Need(Store.Get(pair[0]) is not null,"Charakter nicht gefunden.");if(pair[1]=="allow"){Need(h.Visitors.Count<30,"Besuchsliste voll.");h.Visitors.Add(pair[0]);}else h.Visitors.Remove(pair[0]);}
            return "Besuchsrechte aktualisiert.";
        }
        Need(Math.Abs(s.X-def!.X)<8.8&&Math.Abs(s.Z-def.Z)<5.8,"Diese Aktion benötigt Anwesenheit in deiner Wohnung.");
        if(kind is not ("home_take" or "home_remove" or "home_gear_take"))Need(h.RentUntil>Utc,"Miete überfällig. Am Wohnamt oder im Menü bezahlen.");
        switch(kind){
            case "home_gear_put":Need(s.Gear.ContainsKey(text)&&!s.EquippedGear.Values.Contains(text)&&h.GearStorage.Count<48,"Instanz ablegen oder Lager voll.");h.GearStorage[text]=s.Gear[text];s.Gear.Remove(text);return "Instanz sicher eingelagert.";
            case "home_gear_take":Need(h.GearStorage.ContainsKey(text)&&CharacterRules.Slots(s,Catalog)<Catalog.InventorySlots&&!s.Gear.ContainsKey(text),"Instanz fehlt oder Inventar voll.");s.Gear[text]=h.GearStorage[text];h.GearStorage.Remove(text);return "Instanz zurückgenommen.";
            case "home_expand":Need(h.Rooms==1,"Zweites Zimmer bereits offen.");Take(s,"wood",8);Coins(s,-20);h.Rooms=2;return "Angrenzendes Zimmer geöffnet: gleiche Welt, zusätzliche Fläche.";
            case "home_place":case "home_move":
                var parts=text.Split('|');Need(parts.Length==4,"Möbel-ID/Item, X, Z und Drehung fehlen.");
                Need(double.TryParse(parts[1],NumberStyles.Float,CultureInfo.InvariantCulture,out var x)&&double.IsFinite(x)&&double.TryParse(parts[2],NumberStyles.Float,CultureInfo.InvariantCulture,out var z)&&double.IsFinite(z)&&double.TryParse(parts[3],NumberStyles.Float,CultureInfo.InvariantCulture,out var yaw)&&double.IsFinite(yaw),"Ungültige Möbelposition.");
                // Reparse keeps definite assignment explicit after the checked input boundary.
                x=double.Parse(parts[1],CultureInfo.InvariantCulture);z=double.Parse(parts[2],CultureInfo.InvariantCulture);yaw=double.Parse(parts[3],CultureInfo.InvariantCulture);
                Need(Math.Abs(x-def.X)<7.5&&Math.Abs(z-def.Z)<4.5&&(h.Rooms>1||x<def.X-1.2)&&Distance(x,z,def.X-4,def.Z+5)>2,"Möbel innerhalb freier eigener Wohnfläche platzieren; Eingang freihalten.");
                var id=kind=="home_place"?Guid.NewGuid().ToString("N"):parts[0];
                Need(!h.Furniture.Any(f=>f.Key!=id&&Distance(f.Value.X,f.Value.Z,x,z)<1.3),"Möbel überschneiden sich.");
                if(kind=="home_place"){Need(parts[0]=="oak_chair"&&h.Furniture.Count<30,"Nur vorhandene Möbel, maximal 30.");Take(s,parts[0],1);h.Furniture[id]=new FurnitureState();}
                Need(h.Furniture.ContainsKey(id),"Möbel nicht gefunden.");h.Furniture[id].X=x;h.Furniture[id].Z=z;h.Furniture[id].Yaw=Math.IEEERemainder(yaw,Math.Tau);return "Möbelposition synchronisiert und gespeichert.";
            case "home_remove":Need(h.Furniture.TryGetValue(text,out var furniture),"Möbel nicht gefunden.");Give(s,furniture!.Item,1);h.Furniture.Remove(text);return "Möbel ohne Duplikation zurück im Inventar.";
            case "home_put":case "home_take":
                var transfer=text.Split('|');Need(transfer.Length==2,"Item und Menge fehlen.");var count=Number(transfer[1],1,99);var item=transfer[0];Need(Catalog.Items.ContainsKey(item),"Unbekanntes Item.");
                if(kind=="home_put"){Need(!s.Equipment.Values.Contains(item),"Ausgerüstetes Item ablegen.");Take(s,item,count);Need(h.Storage.GetValueOrDefault(item)+count<=999,"Lagerlimit 999 je Item.");h.Storage[item]=h.Storage.GetValueOrDefault(item)+count;}
                else{Need(h.Storage.GetValueOrDefault(item)>=count,"Lagerbestand fehlt.");Give(s,item,count);h.Storage[item]-=count;if(h.Storage[item]==0)h.Storage.Remove(item);}return "Wohnungslager aktualisiert.";
            case "home_craft":Need(CharacterRules.Craft(s,text,Catalog),"Rezept, Material oder Inventarplatz fehlt.");CharacterRules.Progress(s,"craft",text,Catalog);return "An eigener Werkstatt hergestellt.";
            case "home_breed":Need(PetRules.Breed(s,text,Catalog),"Zucht braucht zwei erwachsene Pets und Futter.");return "Wohnungs-Zuchtbereich genutzt.";
            case "home_plant":Need(h.HarvestAt==0,"Garten bereits bepflanzt.");Take(s,"herb",1);h.HarvestAt=Utc+300;return "Garten angelegt. Ernte in fünf echten Minuten, auch nach Neustart.";
            case "home_harvest":Need(h.HarvestAt>0&&h.HarvestAt<=Utc,"Noch keine reife Ernte.");Give(s,"herb",3);h.HarvestAt=0;return "Gartenertrag einmalig erhalten.";
            default:throw new GameRuleException("Unbekannte Wohnaktion.");
        }
    }
}
