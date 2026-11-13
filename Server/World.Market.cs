namespace MMORPG.Server;
public sealed partial class World
{
    private string MarketAction(SharedWorld w,CharacterRecord c,string kind,string target,string text)
    {
        var s=c.State;Need(Distance(s.X,s.Z,30,12)<=4,"Auktionsbörse bei (30,12). Suche auch aus der Ferne möglich.");
        switch(kind){
            case "market_list":
                var parts=text.Split('|');Need(parts.Length==3&&Catalog.Items.ContainsKey(parts[0]),"Item, Menge und Gesamtpreis fehlen.");
                var count=Number(parts[1],1,99);var price=Number(parts[2],1,100000);var item=parts[0];
                Need(w.Auctions.Count<200&&w.Auctions.Values.Count(a=>a.Seller==c.Id)<10,"Angebotslimit erreicht.");Need(!s.Equipment.Values.Contains(item),"Ausgerüstetes Item ablegen.");Take(s,item,count);
                w.Auctions[Guid.NewGuid().ToString("N")]=new AuctionState{Seller=c.Id,Item=item,Count=count,Price=price,Created=Utc};return "Angebot im persistenten Escrow. 5 % Verkaufsgebühr, mindestens 1 Münze.";
            case "market_buy":
                Need(w.Auctions.TryGetValue(target,out var listing)&&listing.Seller!=c.Id,"Angebot nicht mehr verfügbar oder eigenes Angebot.");var offer=listing!;
                var net=offer.Price-Math.Max(1,(offer.Price+19)/20);Need((long)w.Proceeds.GetValueOrDefault(offer.Seller)+net<=int.MaxValue,"Verkäufer-Erlöslimit erreicht.");
                Coins(s,-offer.Price);Give(s,offer.Item,offer.Count);w.Proceeds[offer.Seller]=w.Proceeds.GetValueOrDefault(offer.Seller)+net;w.Auctions.Remove(target);return "Kauf atomar abgeschlossen; Erlös für Verkäufer abrufbar.";
            case "market_cancel":Need(w.Auctions.TryGetValue(target,out var mine)&&mine.Seller==c.Id,"Kein eigenes aktives Angebot.");Give(s,mine!.Item,mine.Count);w.Auctions.Remove(target);return "Angebot zurückgenommen; Item einmalig zurückgegeben.";
            case "market_claim":var proceeds=w.Proceeds.GetValueOrDefault(c.Id);Need(proceeds>0,"Keine offenen Erlöse.");Coins(s,proceeds);w.Proceeds.Remove(c.Id);return "Verkaufserlöse einmalig ausgezahlt.";
            default:throw new GameRuleException("Unbekannte Marktaktion.");
        }
    }
}
