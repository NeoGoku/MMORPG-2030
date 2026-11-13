namespace MMORPG.Server;
public sealed partial class World
{
    private string GuildAction(SharedWorld w,CharacterRecord c,string kind,string target,string text)
    {
        var s=c.State;var g=GuildOf(w,c.Id);
        if(kind=="guild_create"){
            var name=text.Trim();Need(Distance(s.X,s.Z,30,24)<=3,"Gildenregister bei Elin (30,24).");
            Need(g is null&&w.Guilds.Count<100&&name.Length is >=3 and <=24&&name.All(ch=>char.IsLetterOrDigit(ch)||ch==' '),"Name: 3–24 Buchstaben/Ziffern; keine bestehende Gilde.");
            Need(!w.Guilds.Values.Any(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase)),"Gildenname vergeben.");Coins(s,-20);
            w.Guilds[Guid.NewGuid().ToString("N")]=new GuildState{Name=name,Leader=c.Id,Members=new(){{c.Id,"leader"}}};return "Gilde gegründet.";
        }
        if(kind=="guild_accept"){
            Need(g is null&&w.Guilds.TryGetValue(target,out var invited)&&invited.Invitations.GetValueOrDefault(c.Id)>Utc,"Keine gültige Einladung oder bereits Mitglied.");
            var destination=w.Guilds[target];Need(destination.Members.Count<40,"Gilde voll.");destination.Members[c.Id]="member";destination.Invitations.Remove(c.Id);return "Gilde beigetreten.";
        }
        Need(g is not null,"Keine Gilde.");var guild=g!;var leader=guild.Leader==c.Id;var officer=leader||guild.Members[c.Id]=="officer";
        switch(kind){
            case "guild_release":Need(leader,"Nur die Leitung darf das Haus freigeben.");guild.Hall="";return "Gildenhaus freigegeben; Lager bleibt erhalten.";
            case "guild_invite":Need(officer&&Store.Get(target) is not null&&GuildOf(w,target) is null,"Einladung nur durch Leitung/Offizier an gildenlosen Charakter.");foreach(var expired in guild.Invitations.Where(i=>i.Value<=Utc).Select(i=>i.Key).ToArray())guild.Invitations.Remove(expired);
                Need(guild.Invitations.Count<50||guild.Invitations.ContainsKey(target),"Zu viele Einladungen.");guild.Invitations[target]=Utc+86400;return "Gildeneinladung gespeichert (24 Stunden).";
            case "guild_kick":Need(officer&&target!=guild.Leader&&guild.Members.ContainsKey(target)&&(leader||guild.Members[target]=="member"),"Keine Berechtigung.");guild.Members.Remove(target);return "Mitglied entfernt.";
            case "guild_rank":Need(leader&&guild.Members.ContainsKey(target)&&target!=c.Id&&text is "member" or "officer" or "leader","Nur Leitung darf Ränge ändern.");
                if(text=="leader"){guild.Members[c.Id]="officer";guild.Leader=target;}guild.Members[target]=text;return "Rang geändert.";
            case "guild_leave":Need(!leader||guild.Members.Count==1,"Leitung erst übertragen.");Need(!leader||guild.Storage.Values.Sum()==0&&guild.GearStorage.Count==0&&guild.Hall=="","Lager leeren; Gilde mit Haus kann nicht aufgelöst werden.");guild.Members.Remove(c.Id);if(guild.Members.Count==0)w.Guilds.Remove(w.Guilds.Single(x=>x.Value==guild).Key);return "Gilde verlassen.";
            case "guild_quest":Take(s,"wood",3);Take(s,"ore",2);guild.Quest++;guild.Exp=checked(guild.Exp+25);guild.Influence=checked(guild.Influence+25);Coins(s,5);return "Gildenauftrag Materialversorgung erfüllt: +25 Gilden-EXP, +5 Münzen. Materialien verbraucht.";
            case "guild_buff":Need(officer&&guild.Influence>=25&&guild.BuffUntil<=Utc,"Offizier, 25 Gildenenergie und abgelaufenen Buff benötigt.");guild.Influence-=25;guild.BuffUntil=Utc+300;return "Gildensegen: +2 PvE-Angriff für fünf Minuten.";
            case "guild_hall":Need(leader&&guild.Hall==""&&district.Homes.Any(h=>h.Id==target&&h.GuildHall)&&!w.Guilds.Values.Any(x=>x.Hall==target),"Freies Gildenhaus und Leitung benötigt.");
                Need(guild.Storage.GetValueOrDefault("wood")>=20&&guild.Storage.GetValueOrDefault("ore")>=10,"Haus kostet 20 Holz und 10 Erz aus dem Gildenlager.");guild.Storage["wood"]-=20;guild.Storage["ore"]-=10;guild.Hall=target;return "Physisches Gildenhaus erworben. Alle Mitglieder haben Zutritt.";
            case "guild_gear_put":case "guild_gear_take":
                var gearHall=district.Homes.FirstOrDefault(h=>h.Id==guild.Hall);Need(Distance(s.X,s.Z,30,24)<=3||gearHall is not null&&Math.Abs(s.X-gearHall.X)<9&&Math.Abs(s.Z-gearHall.Z)<6,"Gildenlager am Register oder im eigenen Haus.");
                if(kind=="guild_gear_put"){Need(s.Gear.TryGetValue(target,out var instance)&&!s.EquippedGear.Values.Contains(target)&&guild.GearStorage.Count<48,"Instanz ablegen oder Lagerlimit erreicht.");guild.GearStorage[target]=SharedWorld.Copy(instance!);s.Gear.Remove(target);}
                else{Need(officer&&guild.GearStorage.TryGetValue(target,out var stored)&&CharacterRules.Slots(s,Catalog)<Catalog.InventorySlots&&!s.Gear.ContainsKey(target),"Nur Offizier/Leitung, freie Tasche und vorhandene Instanz.");s.Gear[target]=SharedWorld.Copy(guild.GearStorage[target]);guild.GearStorage.Remove(target);}return "Gilden-Instanzlager atomar aktualisiert.";
            case "guild_put":case "guild_take":
                var parts=text.Split('|');Need(parts.Length==2&&Catalog.Items.ContainsKey(parts[0]),"Item und Menge fehlen.");var item=parts[0];var count=Number(parts[1],1,99);
                var hall=district.Homes.FirstOrDefault(h=>h.Id==guild.Hall);Need(Distance(s.X,s.Z,30,24)<=3||hall is not null&&Math.Abs(s.X-hall.X)<9&&Math.Abs(s.Z-hall.Z)<6,"Gildenlager am Register oder im eigenen Gildenhaus.");
                if(kind=="guild_put"){Need(!s.Equipment.Values.Contains(item),"Ausgerüstetes Item ablegen.");Take(s,item,count);Need(guild.Storage.GetValueOrDefault(item)+count<=999,"Lagerlimit erreicht.");guild.Storage[item]=guild.Storage.GetValueOrDefault(item)+count;}
                else{Need(officer&&guild.Storage.GetValueOrDefault(item)>=count,"Nur Leitung/Offizier mit ausreichendem Bestand.");Give(s,item,count);guild.Storage[item]-=count;if(guild.Storage[item]==0)guild.Storage.Remove(item);}return "Gildenlager atomar aktualisiert.";
            default:throw new GameRuleException("Unbekannte Gildenaktion.");
        }
    }
    private int GuildBonus(Player p)=>GuildOf(Shared,p.Id)?.BuffUntil>Utc?2:0;
}
