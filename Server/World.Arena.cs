namespace MMORPG.Server;
public sealed partial class World
{
    private sealed class ArenaMatch
    {
        public string Id=Guid.NewGuid().ToString("N"),Mode="duel";
        public string[] A=[],B=[];public Dictionary<string,int> Hp=[];
        public Dictionary<string,double> NextHit=[];public Dictionary<string,double> Effects=[];public Dictionary<string,double> SkillReady=[];public Dictionary<string,int> Energy=[];public double Until;
    }
    private readonly List<ArenaMatch> arenaMatches=[];
    private readonly Dictionary<string,(string From,string Mode,double Until)> arenaInvites=[];
    private readonly HashSet<string> arenaReady=[];
    private ArenaMatch? ArenaOf(string id)=>arenaMatches.FirstOrDefault(m=>m.Hp.ContainsKey(id));
    private static bool InArena(Player p)=>p.X>=-62&&p.X<=-38&&p.Z>=23&&p.Z<=47;
    private bool ArenaAction(Player p,string kind,string target,string text="")
    {
        if(!kind.StartsWith("arena_"))return false;
        try{
            Need(p.Data.AccountId!="","Arena benötigt eigene Charaktere.");
            if(kind=="arena_ready"){if(arenaReady.Contains(p.Id))arenaReady.Remove(p.Id);else arenaReady.Add(p.Id);p.Message=arenaReady.Contains(p.Id)?"Bereit für Teamkampf; nur in Arena und nach Annahme beider Kapitäne.":"Team-Bereitschaft zurückgenommen.";return true;}
            if(kind=="arena_leave"){if(ArenaOf(p.Id) is {} old)FinishArena(old,old.A.Contains(p.Id)?old.B:old.A,"Aufgabe");arenaReady.Remove(p.Id);return true;}
            Need(InArena(p),"PvP nur in markierter Arena (-50,35), keine Angriffe außerhalb.");
            if(kind is "arena_invite" or "arena_team_invite" or "arena_war_invite"){
                Need(players.TryGetValue(target,out var peer)&&peer.Id!=p.Id&&peer.Data.AccountId!=""&&peer.Data.AccountId!=p.Data.AccountId&&InArena(peer)&&ArenaOf(p.Id) is null&&ArenaOf(peer.Id) is null,"Beide Spieler müssen frei und mit verschiedenen Accounts in der Arena stehen.");
                var mode=kind=="arena_invite"?"duel":kind=="arena_team_invite"?"team":"war";
                arenaInvites[target]=(p.Id,mode,time+30);p.Message="Arenaanfrage versandt (30 Sekunden).";peer!.Message=p.Data.Name+" fordert "+mode+". Im Arena-Menü annehmen.";return true;
            }
            if(kind=="arena_accept"){
                Need(arenaInvites.TryGetValue(p.Id,out var invitation)&&invitation.From==target&&invitation.Until>=time&&players.TryGetValue(target,out var captain)&&InArena(captain),"Keine gültige Arenaanfrage.");
                var request=arenaInvites[p.Id];var first=players[target];
                string[] Team(Player captain){var party=PartyOf(captain.Id);Need(party is not null&&party.Leader==captain.Id,"Teamkampf benötigt einen Gruppenleiter.");var ids=party!.Members.Keys.Where(players.ContainsKey).ToArray();Need(ids.Length is >=2 and <=5&&ids.All(id=>arenaReady.Contains(id)&&InArena(players[id])),"Alle 2–5 Gruppenmitglieder müssen bereit in der Arena stehen.");return ids;}
                var a=request.Mode=="duel"?new[]{target}:Team(first);var b=request.Mode=="duel"?new[]{p.Id}:Team(p);
                Need(!a.Intersect(b).Any()&&a.Concat(b).All(id=>ArenaOf(id) is null)&&a.Concat(b).Select(id=>players[id].Data.AccountId).Distinct().Count()==a.Length+b.Length,"Keine doppelten Spieler/Accounts oder belegten Kämpfer.");
                if(request.Mode=="war"){var ga=GuildOf(Shared,target);var gb=GuildOf(Shared,p.Id);Need(ga is not null&&gb is not null&&ga!=gb&&a.All(ga.Members.ContainsKey)&&b.All(gb.Members.ContainsKey),"Gildenkrieg: zwei verschiedene Gilden, gruppenreine Teams.");}
                var match=new ArenaMatch{Mode=request.Mode,A=a,B=b,Until=time+180,Hp=a.Concat(b).ToDictionary(id=>id,_=>100)};
                match.Energy=match.Hp.Keys.ToDictionary(id=>id,_=>100);arenaMatches.Add(match);arenaInvites.Remove(p.Id);
                foreach(var id in match.Hp.Keys){var fighter=players[id];fighter.Attacking=false;fighter.Target="";StopFarm(fighter);summons.Remove(id);fighter.Message="Arena: 100 normierte Match-HP, 15 Schaden, 0,8 s. PvE-Werte bleiben erhalten.";}
                return true;
            }
            if(kind=="arena_skill"){ArenaCast(p,target,text);return true;}
            Need(kind=="arena_hit","Unbekannte Arenaaktion.");var battle=ArenaOf(p.Id);Need(battle is not null&&battle.Hp[p.Id]>0&&battle.Hp.GetValueOrDefault(target)>0&&players.TryGetValue(target,out var opponent)&&InArena(opponent),"Nur lebende Teilnehmer eines aktiven Matches.");
            var same=battle!.A.Contains(p.Id)==battle.A.Contains(target);Need(!same&&Distance(p.X,p.Z,players[target].X,players[target].Z)<=3.2&&battle.NextHit.GetValueOrDefault(p.Id)<=time,"Ziel außer Reichweite, Teammitglied oder Angriff noch im Cooldown.");
            battle.NextHit[p.Id]=time+.8;battle.Hp[target]=Math.Max(0,battle.Hp[target]-ArenaDamage(battle,p.Id,target,15));p.AttackSerial++;players[target].HurtSerial++;players[target].LastDamage=15;
            if(battle.A.All(id=>battle.Hp[id]==0))FinishArena(battle,battle.B,"Sieg");else if(battle.B.All(id=>battle.Hp[id]==0))FinishArena(battle,battle.A,"Sieg");
        }catch(GameRuleException e){p.Message=e.Message;}
        return true;
    }
    private int ArenaDamage(ArenaMatch m,string from,string to,int raw)=>Math.Max(1,raw+(m.Effects.GetValueOrDefault("power:"+from)>time?4:0)-(m.Effects.GetValueOrDefault("guard:"+to)>time?4:0));
    private void ArenaCast(Player p,string skillId,string target)
    {
        var m=ArenaOf(p.Id);Need(m is not null&&m.Hp[p.Id]>0&&InArena(p),"Aktives Match erforderlich.");
        var skill=Catalog.Classes[p.Data.ClassId].Skills.FirstOrDefault(s=>s.Id==skillId&&s.Enabled);Need(skill is not null,"Unbekannter Klassenskill.");
        var key=p.Id+":"+skillId;Need(m!.Energy[p.Id]>=20&&m.SkillReady.GetValueOrDefault(key)<=time,"Arenaenergie oder Skill-Cooldown fehlt.");
        var allies=m.A.Contains(p.Id)?m.A:m.B;var foes=m.A.Contains(p.Id)?m.B:m.A;
        var offensive=skill!.Kind is "damage" or "dot" or "slow" or "break" or "drain" or "taunt" or "aoe";
        if(offensive)Need(foes.Contains(target)&&m.Hp[target]>0&&players.TryGetValue(target,out var enemy)&&Distance(p.X,p.Z,enemy.X,enemy.Z)<=12,"Gültiges gegnerisches Matchziel in 12 m erforderlich.");
        if(skill.Kind=="explode")Need(m.Effects.GetValueOrDefault("summon:"+p.Id)>time,"Eigene Arena-Beschwörung nötig.");
        m.Energy[p.Id]-=20;m.SkillReady[key]=time+Math.Max(3,skill.Cooldown);p.SkillSerial++;
        switch(skill.Kind){
            case "heal":case "hot":m.Hp[p.Id]=Math.Min(100,m.Hp[p.Id]+25);break;
            case "party_heal":foreach(var id in allies.Where(id=>m.Hp[id]>0))m.Hp[id]=Math.Min(100,m.Hp[id]+18);break;
            case "buff":case "haste":m.Effects["power:"+p.Id]=time+8;break;
            case "party_buff":foreach(var id in allies)m.Effects["guard:"+id]=time+8;break;
            case "cleanse":foreach(var id in allies){m.Effects.Remove("slow:"+id);m.Effects.Remove("dot:"+id);}break;
            case "blink":var dx=-Math.Sin(p.Yaw)*3;var dz=-Math.Cos(p.Yaw)*3;if(InArenaPoint(p.X+dx,p.Z+dz)){Move(p,dx,dz);}break;
            case "summon":m.Effects["summon:"+p.Id]=time+15;break;
            case "explode":Need(m.Effects.GetValueOrDefault("summon:"+p.Id)>time,"Eigene Arena-Beschwörung nötig.");m.Effects.Remove("summon:"+p.Id);foreach(var id in foes.Where(id=>m.Hp[id]>0&&Distance(p.X,p.Z,players[id].X,players[id].Z)<8))m.Hp[id]=Math.Max(0,m.Hp[id]-20);break;
            default:
                var victims=skill.Kind=="aoe"?foes.Where(id=>m.Hp[id]>0&&Distance(players[target].X,players[target].Z,players[id].X,players[id].Z)<=4).ToArray():new[]{target};
                foreach(var id in victims){var amount=ArenaDamage(m,p.Id,id,skill.PvpPower);m.Hp[id]=Math.Max(0,m.Hp[id]-amount);players[id].HurtSerial++;players[id].LastDamage=amount;}
                if(skill.Kind=="drain")m.Hp[p.Id]=Math.Min(100,m.Hp[p.Id]+8);
                if(skill.Kind=="dot")m.Effects["dot:"+target]=time+5;
                if(skill.Kind=="slow")m.Effects["slow:"+target]=time+5;
                if(skill.Kind is "break" or "taunt")m.Effects.Remove("guard:"+target);
                break;
        }
        if(m.A.All(id=>m.Hp[id]==0))FinishArena(m,m.B,"Sieg");else if(m.B.All(id=>m.Hp[id]==0))FinishArena(m,m.A,"Sieg");
    }
    private static bool InArenaPoint(double x,double z)=>x>=-62&&x<=-38&&z>=23&&z<=47;
    private double ArenaSpeed(Player p)=>ArenaOf(p.Id)?.Effects.GetValueOrDefault("slow:"+p.Id)>time?.5:1;
    private void FinishArena(ArenaMatch match,string[] winners,string reason)
    {
        arenaMatches.Remove(match);foreach(var id in match.Hp.Keys){arenaReady.Remove(id);if(players.TryGetValue(id,out var fighter))fighter.Message="Arena beendet: "+reason+(winners.Contains(id)?" · gewonnen.":".");}
        // Forfeits/disconnects/timeout never mint rewards. One winning reward per account/UTC day.
        if(reason!="Sieg")return;
        try{
            var w=SharedWorld.Copy(Shared);var batch=new List<CharacterRecord>();
            foreach(var id in winners.Where(players.ContainsKey)){
                var c=CharacterRules.Clone(Export(players[id]));var day=Utc/86400;
                if(w.ArenaRewards.GetValueOrDefault(c.AccountId)==day)continue;
                Need(CharacterRules.Add(c.State,"arena_token",1,Catalog),"Arena-Sieg, aber Inventar voll; kein Belohnungstransfer.");
                w.ArenaRewards[c.AccountId]=day;batch.Add(c);
            }
            if(match.Mode=="war"&&batch.Count>0&&GuildOf(w,winners[0]) is {} guild)guild.Exp=checked(guild.Exp+25);
            if(batch.Count>0){Store.SaveWorld(w,batch);sharedState=w;foreach(var c in batch)Import(players[c.Id],c);}
        }catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){foreach(var id in winners.Where(players.ContainsKey))players[id].Message="Arena gewonnen; Belohnung nicht gespeichert: "+(e is GameRuleException?e.Message:"Datenbankfehler");}
    }
    private void ArenaTick(){foreach(var match in arenaMatches.ToArray()){
        if(tick%20==0)foreach(var id in match.Hp.Keys.ToArray()){
            match.Energy[id]=Math.Min(100,match.Energy[id]+5);
            if(match.Hp[id]>0&&match.Effects.GetValueOrDefault("dot:"+id)>time)match.Hp[id]=Math.Max(0,match.Hp[id]-3);
            if(match.Hp[id]>0&&match.Effects.GetValueOrDefault("summon:"+id)>time){var enemies=match.A.Contains(id)?match.B:match.A;var target=enemies.FirstOrDefault(e=>match.Hp[e]>0&&players.ContainsKey(e)&&players.ContainsKey(id)&&Distance(players[e].X,players[e].Z,players[id].X,players[id].Z)<=12);if(target is not null)match.Hp[target]=Math.Max(0,match.Hp[target]-4);}
        }
        if(match.A.All(id=>match.Hp[id]==0)){FinishArena(match,match.B,"Sieg");continue;}if(match.B.All(id=>match.Hp[id]==0)){FinishArena(match,match.A,"Sieg");continue;}
        var a=match.A.All(id=>players.TryGetValue(id,out var p)&&InArena(p));var b=match.B.All(id=>players.TryGetValue(id,out var p)&&InArena(p));
        if(!a||!b||time>=match.Until)FinishArena(match,!a?match.B:match.A,"Zone verlassen / Verbindung / Zeitlimit");
    }foreach(var id in arenaInvites.Where(i=>i.Value.Until<time).Select(i=>i.Key).ToArray())arenaInvites.Remove(id);}
    private int VisibleHp(Player p)=>ArenaOf(p.Id)?.Hp.GetValueOrDefault(p.Id)??p.Hp;
    private int VisibleMaxHp(Player p)=>ArenaOf(p.Id) is null?CharacterRules.MaxHp(p.Data,Catalog):100;
    private object ArenaView(Player p)=>new{inZone=InArena(p),ready=arenaReady.Contains(p.Id),invitations=arenaInvites.Where(i=>i.Key==p.Id).Select(i=>new{from=i.Value.From,mode=i.Value.Mode,remaining=Math.Max(0,i.Value.Until-time)}).ToArray(),matches=arenaMatches.Where(m=>m.Hp.ContainsKey(p.Id)||InArena(p)).Select(m=>new{id=m.Id,mode=m.Mode,a=m.A,b=m.B,hp=m.Hp,energy=m.Energy,effects=m.Effects,remaining=Math.Max(0,m.Until-time)}).ToArray()};
}
