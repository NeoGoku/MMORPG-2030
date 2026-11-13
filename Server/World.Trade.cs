namespace MMORPG.Server;

public sealed partial class World
{
    private sealed record Offer(string Item,int Count,int Coins,GearState? Gear=null);
    private sealed class Trade(string a,string b,double expires)
    {
        public string A=a,B=b;public double Expires=expires;
        public Dictionary<string,Offer> Offers=new(){{a,new("",0,0)},{b,new("",0,0)}};
        public HashSet<string> Confirmed=[];
    }
    private readonly List<Trade> trades=[];
    private Trade? TradeOf(string id)=>trades.FirstOrDefault(t=>t.A==id||t.B==id);
    private bool TradeAction(Player p,string kind,string target,string text)
    {
        var trade=TradeOf(p.Id);
        switch(kind){
            case "trade_open":
                if(p.Data.AccountId==""||!players.TryGetValue(target,out var peer)||target==p.Id||peer.Data.AccountId==""||TradeOf(target) is not null||trade is not null||Distance(p.X,p.Z,peer.X,peer.Z)>5){p.Message="Handel braucht zwei angemeldete Spieler in 5 m Reichweite.";break;}
                trades.Add(new(p.Id,target,time+120));p.Message="Handelsfenster geöffnet. Beide Angebote und Bestätigungen nötig.";peer.Message=p.Data.Name+" möchte handeln.";break;
            case "trade_offer":
                if(trade is null){p.Message="Kein Handel.";break;}
                var parts=text.Split('|');
                if(parts.Length!=3||!int.TryParse(parts[1],out var count)||!int.TryParse(parts[2],out var coins)||count<0||count>99||coins<0||coins>100000||coins>p.Coins||
                    (parts[0]!=""&&((!Catalog.Items.ContainsKey(parts[0])&&!p.Data.State.Gear.ContainsKey(parts[0]))||count==0||(p.Data.State.Gear.ContainsKey(parts[0])?count!=1||p.Data.State.EquippedGear.Values.Contains(parts[0]):p.Data.State.Inventory.GetValueOrDefault(parts[0])<count)||p.Data.State.Equipment.Values.Contains(parts[0])))||(parts[0]==""&&count!=0)){
                    p.Message="Ungültiges Angebot oder ausgerüsteter Gegenstand.";break;
                }
                trade.Offers[p.Id]=new(parts[0],count,coins,p.Data.State.Gear.TryGetValue(parts[0],out var offeredGear)?SharedWorld.Copy(offeredGear):null);trade.Confirmed.Clear();trade.Expires=time+120;p.Message="Angebot aktualisiert; Bestätigungen zurückgesetzt.";break;
            case "trade_confirm":
                if(trade is null)break;
                trade.Confirmed.Add(p.Id);
                if(trade.Confirmed.Count<2){p.Message="Warte auf Gegenbestätigung.";break;}
                if(!players.TryGetValue(trade.A,out var a)||!players.TryGetValue(trade.B,out var b)||a.Hp<=0||b.Hp<=0||Distance(a.X,a.Z,b.X,b.Z)>5){trades.Remove(trade);p.Message="Handel abgebrochen: Reichweite/Verbindung.";break;}
                var ca=CharacterRules.Clone(Export(a));var cb=CharacterRules.Clone(Export(b));
                var oa=trade.Offers[a.Id];var ob=trade.Offers[b.Id];
                if(!RemoveOffer(ca.State,oa)||!RemoveOffer(cb.State,ob)||!ReceiveOffer(ca.State,ob)||!ReceiveOffer(cb.State,oa)){trades.Remove(trade);p.Message="Handel abgebrochen: Besitz, Währung oder Inventarplatz geändert.";break;}
                try{Store.SaveBatch([ca,cb]);Import(a,ca);Import(b,cb);a.Message=b.Message="Handel atomar abgeschlossen.";}
                catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){a.Message=b.Message="Handel nicht gespeichert; keine Übertragung.";}
                trades.Remove(trade);break;
            case "trade_cancel":if(trade is not null)trades.Remove(trade);p.Message="Handel abgebrochen.";break;
            default:return false;
        }
        return true;
    }
    private bool RemoveOffer(CharacterState c,Offer offer)
    {
        if(c.Coins<offer.Coins)return false;
        if(offer.Gear is not null){
            if(!c.Gear.TryGetValue(offer.Item,out var current)||c.EquippedGear.Values.Contains(offer.Item)||current.Item!=offer.Gear.Item||current.Condition!=offer.Gear.Condition)return false;
            c.Gear.Remove(offer.Item);c.Coins-=offer.Coins;return true;
        }
        if(offer.Item!=""){
            if(c.Inventory.GetValueOrDefault(offer.Item)<offer.Count||c.Equipment.Values.Contains(offer.Item))return false;
            c.Inventory[offer.Item]-=offer.Count;if(c.Inventory[offer.Item]==0)c.Inventory.Remove(offer.Item);
        }
        c.Coins-=offer.Coins;return true;
    }
    private bool ReceiveOffer(CharacterState c,Offer offer)
    {
        if(c.Coins>int.MaxValue-offer.Coins)return false;
        if(offer.Gear is not null){if(CharacterRules.Slots(c,Catalog)>=Catalog.InventorySlots||c.Gear.ContainsKey(offer.Item))return false;c.Gear[offer.Item]=SharedWorld.Copy(offer.Gear);c.Coins+=offer.Coins;return true;}
        if(offer.Item!=""&&!CharacterRules.Add(c,offer.Item,offer.Count,Catalog))return false;
        c.Coins+=offer.Coins;return true;
    }
    private object? TradeView(Player p)
    {
        var trade=TradeOf(p.Id);if(trade is null)return null;
        return new {partner=trade.A==p.Id?trade.B:trade.A,remaining=Math.Max(0,trade.Expires-time),offers=trade.Offers.Select(o=>new {owner=o.Key,item=o.Value.Item,count=o.Value.Count,coins=o.Value.Coins,confirmed=trade.Confirmed.Contains(o.Key)}).ToArray()};
    }
}
