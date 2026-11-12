namespace MMORPG.Server;

public sealed partial class World
{
    private sealed class Party(string leader)
    {
        public string Id=Guid.NewGuid().ToString("N");
        public string Leader=leader;
        public Dictionary<string,string> Members=[];
    }
    private sealed record ChatLine(long Id,string Kind,string From,string Name,string Text,HashSet<string> Recipients);
    private readonly Dictionary<string,Party> parties=[];
    private readonly Dictionary<string,Dictionary<string,double>> invitations=[];
    private readonly Dictionary<string,double> offlinePartyExpiry=[];
    private readonly Dictionary<string,double> chatDeadlines=[];
    private readonly Queue<ChatLine> chat=[];
    private long chatSequence;
    private Party? PartyOf(string id)=>parties.Values.FirstOrDefault(g=>g.Members.ContainsKey(id));
    private void RemoveMember(Party party,string id)
    {
        party.Members.Remove(id);offlinePartyExpiry.Remove(id);
        if(party.Members.Count==0){parties.Remove(party.Id);return;}
        if(party.Leader==id)party.Leader=party.Members.Keys.FirstOrDefault(players.ContainsKey)??party.Members.Keys.First();
    }
    private bool SocialAction(Player p,string kind,string target,string text)
    {
        var party=PartyOf(p.Id);
        switch(kind)
        {
            case "party_invite":
                if(!players.TryGetValue(target,out var peer)||target==p.Id||PartyOf(target) is not null){p.Message="Spieler nicht verfügbar oder bereits in einer Gruppe.";break;}
                if(party is null){party=new Party(p.Id);party.Members[p.Id]=p.Data.Name;parties[party.Id]=party;}
                if(party.Leader!=p.Id||party.Members.Count>=Math.Min(10,Catalog.PartyLimit)){p.Message="Nur der Gruppenleiter darf einladen; Gruppe eventuell voll.";break;}
                if(!invitations.TryGetValue(target,out var invites))invitations[target]=invites=[];
                if(invites.Count>=5&&!invites.ContainsKey(p.Id)){p.Message="Zu viele offene Einladungen.";break;}
                invites[p.Id]=time+30;p.Message="Einladung versandt.";peer.Message=p.Data.Name+" lädt dich in eine Gruppe ein.";break;
            case "party_accept":
                var invited=PartyOf(target);
                if(party is not null||!invitations.TryGetValue(p.Id,out var pending)||!pending.TryGetValue(target,out var until)||until<time||invited is null||invited.Leader!=target||invited.Members.Count>=Math.Min(10,Catalog.PartyLimit)){p.Message="Einladung abgelaufen oder Gruppe voll.";break;}
                invited.Members[p.Id]=p.Data.Name;invitations.Remove(p.Id);p.Message="Gruppe beigetreten.";break;
            case "party_leave":
                if(party is not null)RemoveMember(party,p.Id);p.Message="Gruppe verlassen.";break;
            case "party_kick":
                if(party is null||party.Leader!=p.Id||!party.Members.ContainsKey(target)||target==p.Id){p.Message="Keine Berechtigung.";break;}
                RemoveMember(party,target);p.Message="Mitglied entfernt.";break;
            case "party_promote":
                if(party is null||party.Leader!=p.Id||!party.Members.ContainsKey(target)||!players.ContainsKey(target)){p.Message="Keine Berechtigung oder Spieler offline.";break;}
                party.Leader=target;p.Message="Gruppenleitung übertragen.";break;
            case "chat_local":case "chat_private":case "chat_party":
                text=new string(text.Where(c=>!char.IsControl(c)).ToArray()).Trim();
                if(text.Length is <1 or >240||chatDeadlines.GetValueOrDefault(p.Id)>time){p.Message="Chat zu schnell oder Nachricht ungültig.";break;}
                HashSet<string> recipients;
                if(kind=="chat_private"){
                    if(!players.ContainsKey(target)){p.Message="Empfänger offline.";break;}
                    recipients=[p.Id,target];
                }else if(kind=="chat_party"){
                    if(party is null){p.Message="Keine Gruppe.";break;}
                    recipients=party.Members.Keys.Where(players.ContainsKey).ToHashSet();
                }else recipients=players.Values.Where(v=>Distance(p.X,p.Z,v.X,v.Z)<=Catalog.LocalChatRadius).Select(v=>v.Id).ToHashSet();
                chatDeadlines[p.Id]=time+.75;
                chat.Enqueue(new(++chatSequence,kind,p.Id,p.Data.Name,text,recipients));while(chat.Count>200)chat.Dequeue();break;
            default:return false;
        }
        return true;
    }
    private object SocialView(Player p)
    {
        var party=PartyOf(p.Id);
        return new {
            trade=TradeView(p),
            party=party is null?null:new {id=party.Id,leader=party.Leader,members=party.Members.Select(m=>new {id=m.Key,name=m.Value,online=players.ContainsKey(m.Key),hp=players.TryGetValue(m.Key,out var v)?v.Hp:0}).ToArray()},
            invitations=invitations.GetValueOrDefault(p.Id)?.Where(i=>i.Value>=time&&PartyOf(i.Key)?.Leader==i.Key).Select(i=>new {leader=i.Key,name=players.GetValueOrDefault(i.Key)?.Data.Name??"Gruppenleiter"}).ToArray()??[],
            chat=chat.Where(c=>c.Recipients.Contains(p.Id)).Select(c=>new {id=c.Id,kind=c.Kind,from=c.From,name=c.Name,text=c.Text}).ToArray(),
            online=players.Values.Where(v=>v.Id!=p.Id).Select(v=>new{id=v.Id,name=v.Data.Name,level=v.Level}).ToArray(),
            nearby=players.Values.Where(v=>v.Id!=p.Id&&Distance(v.X,v.Z,p.X,p.Z)<=Catalog.PartyExpRadius).Select(v=>new{id=v.Id,name=v.Data.Name,level=v.Level}).ToArray()
        };
    }
    private void SocialDisconnect(string id)
    {
        var trade=TradeOf(id);if(trade is not null)trades.Remove(trade);
        var party=PartyOf(id);if(party is not null){offlinePartyExpiry[id]=time+120;if(party.Leader==id)party.Leader=party.Members.Keys.FirstOrDefault(m=>m!=id&&players.ContainsKey(m))??id;}
        invitations.Remove(id);chatDeadlines.Remove(id);
    }
    private void SocialTick()
    {
        trades.RemoveAll(t=>t.Expires<=time||!players.ContainsKey(t.A)||!players.ContainsKey(t.B));
        foreach(var expired in offlinePartyExpiry.Where(e=>e.Value<=time).ToArray()){
            var party=PartyOf(expired.Key);if(party is not null&&!players.ContainsKey(expired.Key))RemoveMember(party,expired.Key);
            offlinePartyExpiry.Remove(expired.Key);
        }
        foreach(var pending in invitations.Values)foreach(var id in pending.Where(i=>i.Value<time).Select(i=>i.Key).ToArray())pending.Remove(id);
    }
    private void RewardKill(Player killer,Monster monster)
    {
        monster.Respawn=time+R.MonsterRespawnSeconds;killer.Attacking=false;
        var party=PartyOf(killer.Id);
        var eligible=players.Values.Where(p=>p.Id==killer.Id||(party is not null&&party.Members.ContainsKey(p.Id)&&p.Hp>0&&Distance(p.X,p.Z,monster.X,monster.Z)<=Catalog.PartyExpRadius)).OrderBy(p=>p.Id).ToArray();
        for(var i=0;i<eligible.Length;i++){
            var p=eligible[i];var amount=R.KillExp/eligible.Length+(i<R.KillExp%eligible.Length?1:0);
            GrantExp(p,amount);GrantPetExp(p);
            if(p.Quest=="active"){p.Kills++;if(p.Kills>=R.QuestKills)p.Quest="ready";}
            var dropId=Guid.NewGuid().ToString("N");loot[dropId]=new(dropId,p.Id,monster.Spawn.Id,monster.X,monster.Z,time+90);
            p.Message=$"Gegner besiegt. +{amount} Gruppen-EXP; persönliche Beute liegt bereit.";
        }
    }
}
