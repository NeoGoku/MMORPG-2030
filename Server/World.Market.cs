namespace MMORPG.Server;
public sealed partial class World
{
    private void ReceiveAuction(CharacterState s,AuctionState offer){if(offer.Pet is not null){Need(s.OwnedPets.Count<Catalog.MaxOwnedPets&&!s.OwnedPets.Any(p=>p.Id==offer.Pet.Id),"Petplätze voll oder Pet bereits vorhanden.");s.OwnedPets.Add(SharedWorld.Copy(offer.Pet));return;}if(offer.Gear is null){Give(s,offer.Item,offer.Count);return;}Need(CharacterRules.Slots(s,Catalog)<Catalog.InventorySlots&&!s.Gear.ContainsKey(offer.GearId),"Inventar voll oder Instanz bereits vorhanden.");s.Gear[offer.GearId]=SharedWorld.Copy(offer.Gear);}
    private string MarketAction(SharedWorld w,CharacterRecord c,string kind,string target,string text,bool atStall=false)
    {
        var s=c.State;Need(atStall||Distance(s.X,s.Z,30,12)<=4,"Auktionsbörse bei (30,12). Suche auch aus der Ferne möglich.");
        switch(kind){
            case "market_pet":
                var pet=s.OwnedPets.FirstOrDefault(p=>p.Id==target);Need(pet is not null&&pet.Id!=s.Pet.Id&&!pet.Active&&pet.TrainingUntil<=0,"Nur nicht ausgewählte, inaktive Pets ohne laufendes Training.");
                var petPrice=Number(text,1,100000);Need(w.Auctions.Count<200&&w.Auctions.Values.Count(a=>a.Seller==c.Id)<10,"Angebotslimit erreicht.");
                w.Auctions[Guid.NewGuid().ToString("N")]=new AuctionState{Seller=c.Id,Item="pet_egg",Count=1,Price=petPrice,Created=Utc,Pet=SharedWorld.Copy(pet!)};s.OwnedPets.Remove(pet!);return "Pet mit Identität/Eigenschaften im Escrow.";
            case "market_list":
                var parts=text.Split('|');Need(parts.Length==3&&(Catalog.Items.ContainsKey(parts[0])||s.Gear.ContainsKey(parts[0])),"Item, Menge und Gesamtpreis fehlen.");
                var count=Number(parts[1],1,99);var price=Number(parts[2],1,100000);var item=parts[0];
                Need(w.Auctions.Count<200&&w.Auctions.Values.Count(a=>a.Seller==c.Id)<10,"Angebotslimit erreicht.");Need(!s.Equipment.Values.Contains(item)&&!s.EquippedGear.Values.Contains(item),"Ausgerüstetes Item ablegen.");
                GearState? gear=null;var gearId="";
                if(s.Gear.TryGetValue(item,out var instance)){Need(count==1,"Eine Instanz je Angebot.");gear=SharedWorld.Copy(instance);gearId=item;s.Gear.Remove(item);item=gear.Item;}else Take(s,item,count);
                w.Auctions[Guid.NewGuid().ToString("N")]=new AuctionState{Gear=gear,GearId=gearId,Seller=c.Id,Item=item,Count=count,Price=price,Created=Utc};return "Angebot im persistenten Escrow. 5 % Verkaufsgebühr, mindestens 1 Münze.";
            case "market_buy":
                Need(w.Auctions.TryGetValue(target,out var listing)&&listing.Seller!=c.Id&&listing.Created+Catalog.AuctionDays*86400L>Utc,"Angebot nicht mehr verfügbar oder eigenes Angebot.");var offer=listing!;Need(atStall||offer.StallId=="","Dieses Angebot am physischen Stand kaufen.");
                var net=offer.Price-Math.Max(1,(offer.Price+19)/20);Need((long)w.Proceeds.GetValueOrDefault(offer.Seller)+net<=int.MaxValue,"Verkäufer-Erlöslimit erreicht.");
                Coins(s,-offer.Price);ReceiveAuction(s,offer);w.Proceeds[offer.Seller]=w.Proceeds.GetValueOrDefault(offer.Seller)+net;w.Auctions.Remove(target);return "Kauf atomar abgeschlossen; Erlös für Verkäufer abrufbar.";
            case "market_cancel":Need(w.Auctions.TryGetValue(target,out var mine)&&mine.Seller==c.Id,"Kein eigenes aktives Angebot.");ReceiveAuction(s,mine!);w.Auctions.Remove(target);return "Angebot zurückgenommen; Item einmalig zurückgegeben.";
            case "market_claim":var proceeds=w.Proceeds.GetValueOrDefault(c.Id);Need(proceeds>0,"Keine offenen Erlöse.");Coins(s,proceeds);w.Proceeds.Remove(c.Id);return "Verkaufserlöse einmalig ausgezahlt.";
            default:throw new GameRuleException("Unbekannte Marktaktion.");
        }
    }
}
