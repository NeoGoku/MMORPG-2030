namespace MMORPG.Server;
public sealed partial class World
{
    private string StallAction(SharedWorld w,CharacterRecord c,string kind,string target,string text)
    {
        var s=c.State;
        if(kind=="stall_buy"){
            Need(w.Auctions.TryGetValue(target,out var offer)&&offer.StallId!=""&&w.Stalls.TryGetValue(offer.StallId,out var stand)&&stand.Until>Utc,"Stand oder Angebot nicht verfügbar.");
            var location=w.Stalls[w.Auctions[target].StallId];Need(Distance(s.X,s.Z,location.X,location.Z)<=4,"Zum Stand gehen (4 m Reichweite).");return MarketAction(w,c,"market_buy",target,"",true);
        }
        if(kind=="stall_open"){
            var name=text.Trim();Need(name.Length is >=3 and <=28&&name.All(ch=>char.IsLetterOrDigit(ch)||ch is ' ' or '-' or '_'),"Standname: 3–28 Buchstaben/Ziffern.");
            Need(s.X is >=27 and <=42&&s.Z is >=16 and <=25&&Distance(s.X,s.Z,30,24)>=3,"Stände nur in Handelszone x=27–42, z=16–25; Register freihalten.");
            Need(w.Stalls.Count<Catalog.MaxStalls||w.Stalls.ContainsKey(c.Id),"Standlimit erreicht.");Need(w.Stalls.Where(p=>p.Key!=c.Id&&p.Value.Until>Utc).All(p=>Distance(p.Value.X,p.Value.Z,s.X,s.Z)>=3),"Drei Meter Abstand zum nächsten Stand.");
            w.Stalls[c.Id]=new StallState{Title=name,X=s.X,Z=s.Z,Until=Utc+Catalog.StallSeconds};return "Stand geöffnet. Angebote nutzen persistentes Escrow, auch während du offline bist.";
        }
        Need(w.Stalls.TryGetValue(c.Id,out var own),"Kein eigener Stand.");Need(Distance(s.X,s.Z,own!.X,own.Z)<=4,"Eigenen Stand besuchen.");
        if(kind=="stall_close"){foreach(var offer in w.Auctions.Values.Where(o=>o.StallId==c.Id))offer.StallId="";w.Stalls.Remove(c.Id);return "Stand geschlossen. Restliche Angebote bleiben an der Auktionsbörse im Escrow.";}
        Need(own.Until>Utc,"Stand abgelaufen. Neu eröffnen oder Restangebote an der Börse zurücknehmen.");
        if(kind=="stall_claim")return MarketAction(w,c,"market_claim","","",true);
        if(kind=="stall_list"){
            var previous=w.Auctions.Keys.ToHashSet();var message=MarketAction(w,c,"market_list","",text,true);var added=w.Auctions.Single(a=>!previous.Contains(a.Key));added.Value.StallId=c.Id;return message;
        }
        throw new GameRuleException("Unbekannte Standaktion.");
    }
}
