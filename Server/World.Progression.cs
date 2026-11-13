using System.Numerics;

namespace MMORPG.Server;

public sealed partial class World
{
    private readonly GameCatalog Catalog=game??GameCatalog.Load(Path.Combine(AppContext.BaseDirectory,"game.json"));
    private readonly IGameStore Store=persistence??new MemoryGameStore();
    private readonly Dictionary<string,CharacterRecord> pendingSaves=[];
    private readonly double wallStart=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000.0;
    public string? JoinCharacter(CharacterRecord record)
    {
        lock(gate){cellsDirty=true;if(players.Count>=100||players.ContainsKey(record.Id))return null;
            // Never reconnect from the older SQL row while a newer disconnect snapshot is pending.
            if(pendingSaves.TryGetValue(record.Id,out var pending)){
                if(pending.AccountId!=record.AccountId)return null;
                try{Store.Save(pending);pendingSaves.Remove(record.Id);}
                catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){return null;}
            }
            if(record.AccountId!=""){var latest=Store.Get(record.Id);if(latest is null||latest.AccountId!=record.AccountId)return null;record=latest;}
            PetRules.CompleteAll(record,DateTimeOffset.UtcNow.ToUnixTimeSeconds(),Catalog);
            var p=new Player(record.Id,record.State.X,record.State.Z,100);Import(p,record);
            if(!district.Walkable(p.X,p.Z)||!HomeAccess(p,p.X,p.Z)||!RegionAccess(p,p.X,p.Z)){p.X=district.Spawn.X;p.Z=district.Spawn.Z;}
            p.Hp=Math.Clamp(record.State.Hp,0,CharacterRules.MaxHp(record,Catalog));p.PetX=p.X+1;p.PetZ=p.Z+1;players[p.Id]=p;return p.Id;}
    }
    private CharacterRecord Export(Player p)
    {
        var c=p.Data.State;c.Level=p.Level;c.Exp=p.Exp;c.StatPoints=p.StatPoints;c.Coins=p.Coins;c.Scrap=p.Scrap;c.Kills=p.Kills;c.Quest=p.Quest;c.X=p.X;c.Z=p.Z;c.Mana=p.Mana;c.Hp=p.Hp;c.Timers=p.Cooldowns.ToDictionary(k=>k.Key,k=>wallStart+k.Value);c.Timers["pet"]=wallStart+p.NextPet;c.Timers["respawn"]=wallStart+p.Respawn;c.BuffExpires=wallStart+p.BuffUntil;
        PetRules.Normalize(p.Data);return p.Data;
    }
    private void Import(Player p,CharacterRecord d)
    {p.Data=d;var c=d.State;p.X=c.X;p.Z=c.Z;p.Level=c.Level;p.Exp=c.Exp;p.StatPoints=c.StatPoints;p.Coins=c.Coins;p.Scrap=c.Scrap;p.Kills=c.Kills;p.Quest=c.Quest;p.Mana=c.Mana;p.Hp=c.Hp;p.Cooldowns=c.Timers.ToDictionary(k=>k.Key,k=>k.Value-wallStart);p.NextPet=c.Timers.GetValueOrDefault("pet")-wallStart;p.Respawn=c.Timers.GetValueOrDefault("respawn")-wallStart;p.BuffUntil=c.BuffExpires-wallStart;}
    private int Damage(Player p)=>GuildBonus(p)+CharacterRules.Attack(Export(p),Catalog)+(p.BuffUntil>time?Catalog.BuffAttack:0)-(p.Cooldowns.GetValueOrDefault("status:weaken")>time?Catalog.StatusPenalty:0);
    private void CommitAction(Player p,CharacterRecord previous,int oldHp,Dictionary<string,Drop> oldLoot,Dictionary<string,Monster> oldMonsters,Dictionary<string,CharacterRecord> oldPeers,Dictionary<string,Summon> oldSummons,FarmState oldFarm)
    {
        if(p.Data.AccountId=="")return;
        try{Store.SaveBatch(oldPeers.Keys.Select(id=>players[id]).Where(v=>v.Data.AccountId!="").Select(Export).Append(Export(p)).ToArray());p.SavedTick=tick;foreach(var id in oldPeers.Keys)players[id].SavedTick=tick;if(p.Message=="Aktion erfolgreich: save_now")p.Message=Store.Durable?"Charakterzustand dauerhaft bestätigt.":"RAM-Test: nur flüchtig gespeichert.";}catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){Import(p,previous);foreach(var peer in oldPeers)Import(players[peer.Key],peer.Value);p.Hp=oldHp;p.Farm=oldFarm;loot.Clear();foreach(var item in oldLoot)loot[item.Key]=item.Value;monsters.Clear();foreach(var item in oldMonsters)monsters[item.Key]=item.Value;summons.Clear();foreach(var item in oldSummons)summons[item.Key]=item.Value;p.Message="Speichern fehlgeschlagen; Aktion zurückgesetzt. Bitte erneut versuchen.";}
    }
    private double checkpointRetryAt;private int checkpointFailures;
    private void Checkpoint(bool force=false)
    {
        if(!force&&time<checkpointRetryAt)return;
        var failed=false;
        foreach(var group in players.Values.Where(p=>p.Data.AccountId!="").GroupBy(p=>PartyOf(p.Id)?.Id??p.Id)){
            var members=group.ToArray();
            try{Store.SaveBatch(members.Select(Export).ToArray());foreach(var p in members)p.SavedTick=tick;}
            catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){failed=true;foreach(var p in members)p.Message="Speicherung ausstehend; letzter bestätigter Stand unverändert.";}
        }
        foreach(var pair in pendingSaves.ToArray())try{Store.Save(pair.Value);pendingSaves.Remove(pair.Key);}catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){failed=true;}
        checkpointFailures=failed?Math.Min(5,checkpointFailures+1):0;checkpointRetryAt=failed?time+Math.Min(30,Math.Pow(2,checkpointFailures)):time;
    }
    private bool Near(Player p,string service)=>Catalog.ServiceLocations(service).Any(point=>Distance(p.X,p.Z,point.X,point.Z)<=3);
    private bool ExtendedAction(Player p,string kind,string target)
    {
        if(FarmAction(p,kind,target))return true;
        var c=Export(p).State;var ok=false;
        switch(kind){
            case "appearance":if(AppearanceRules.Valid(target)){p.Data.Appearance=target;ok=true;}break;
            case "gear_unpack":ok=GearRules.Unpack(c,target,Catalog);break;
            case "gear_equip":ok=GearRules.Equip(p.Data,target,Catalog);break;
            case "gear_legacy_remove":ok=c.Equipment.Remove(target);break;
            case "gear_remove":ok=c.EquippedGear.Remove(target);break;
            case "gear_repair":ok=Near(p,"merchant")&&GearRules.Repair(c,target,Catalog);break;
            case "save_now":ok=true;break;
            case "slot":
                var binding=target.Split('|');
                if(binding.Length==2&&int.TryParse(binding[0],out var index)&&index>=0&&index<6&&Catalog.Classes[p.Data.ClassId].Skills.Any(s=>s.Id==binding[1])){while(c.SkillBar.Count<6)c.SkillBar.Add("skill1");c.SkillBar[index]=binding[1];ok=true;}break;
            case "reset_stats":ok=CharacterRules.Reset(c,Catalog);break;
            case "prestige":ok=Near(p,"merchant")&&CharacterRules.PrestigeUp(c,Catalog);break;
            case "stat":ok=CharacterRules.Allocate(c,target,Catalog);break;
            case "farm_stat":ok=CharacterRules.FarmPoint(c,Catalog);break;
            case "buy":ok=Near(p,"merchant")&&CharacterRules.Buy(c,target,Catalog);break;
            case "sell":ok=Near(p,"merchant")&&CharacterRules.Sell(c,target,Catalog);break;
            case "equip":ok=CharacterRules.Equip(c,target,Catalog,p.Data.ClassId);break;
            case "craft":ok=Near(p,"workbench")&&CharacterRules.Craft(c,target,Catalog);if(ok)CharacterRules.Progress(c,"craft",target,Catalog);break;
            case "gather":
                var node=Catalog.Nodes.FirstOrDefault(n=>n.Id==target);
                if(node is not null&&1+c.Professions.GetValueOrDefault(node.Profession)/Catalog.ProfessionLevelXp>=node.MinProfessionLevel&&Distance(p.X,p.Z,node.X,node.Z)<=3&&p.Cooldowns.GetValueOrDefault("gather:"+target)<=time&&CharacterRules.Add(c,node.Item,1,Catalog)){
                    p.Cooldowns["gather:"+target]=time+5;c.Professions[node.Profession]=c.Professions.GetValueOrDefault(node.Profession)+Catalog.ProfessionGatherXp;c.FarmXp=(BigInteger.Parse(c.FarmXp)+10).ToString();CharacterRules.Progress(c,"gather",target,Catalog);ok=true;
                }break;
            case "claim":ok=Near(p,"merchant")&&CharacterRules.Claim(c,target,Catalog,p.Data.ClassId);break;
            case "use":if(target=="potion"&&c.Inventory.GetValueOrDefault(target)>0&&p.Hp<CharacterRules.MaxHp(p.Data,Catalog)&&p.Cooldowns.GetValueOrDefault("potion")<=time){c.Inventory[target]--;if(c.Inventory[target]==0)c.Inventory.Remove(target);p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+Catalog.Items[target].Heal);p.Cooldowns["potion"]=time+3;ok=true;}break;
            case "pet":if(target is "combat" or "heal" or "loot" or "tank" or "buff" or "gather"){c.Pet.Active=c.Pet.TrainingUntil<=0;c.Pet.Mode=target;p.PetX=p.X+1;p.PetZ=p.Z+1;ok=true;}else if(target=="off"){c.Pet.Active=false;ok=true;}break;
            case "pet_hatch":ok=Near(p,"workbench")&&PetRules.Hatch(c,Catalog);break;
            case "pet_breed":ok=Near(p,"workbench")&&PetRules.Breed(c,target,Catalog);break;
            case "pet_evolve":ok=Near(p,"workbench")&&PetRules.Evolve(c,Catalog);break;
            case "pet_train":ok=Near(p,"workbench")&&PetRules.Train(c,DateTimeOffset.UtcNow.ToUnixTimeSeconds(),Catalog);break;
            case "pet_select":ok=PetRules.Select(c,target);break;
            case "pet_take":ok=PetRules.TakeBag(c,Catalog);break;
            case "world_transit":ok=WorldTransit(p,target);break;
            case "forest_transit":if(Distance(p.X,p.Z,0,90)<=3&&p.Level>=10){p.X=0;p.Z=112;ok=true;}else if(Distance(p.X,p.Z,0,112)<=3){p.X=0;p.Z=90;ok=true;}break;
            case "transit":if(Distance(p.X,p.Z,0,-40)<=3&&p.Level>=5&&p.Quest=="complete"){p.X=0;p.Z=-59;ok=true;}else if(Distance(p.X,p.Z,0,-59)<=3){p.X=0;p.Z=-40;ok=true;}break;
            case "teleport":if(Near(p,"teleporter")||Near(p,"return")){p.X=district.Spawn.X;p.Z=p.Z<30?Catalog.Services["return"].Z:district.Spawn.Z;ok=true;}break;
            case "skill":return Cast(p,target);
            default:return false;
        }
        if(ok){var px=p.X;var pz=p.Z;p.Level=c.Level;p.Exp=c.Exp;p.StatPoints=c.StatPoints;p.Coins=c.Coins;p.Scrap=c.Scrap;p.X=px;p.Z=pz;p.Message="Aktion erfolgreich: "+kind;}else p.Message="Nicht möglich: Reichweite, Voraussetzungen, Material, Platz oder Cooldown prüfen.";
        return true;
    }
    private bool Cast(Player p,string id)
    {
        if(ArenaOf(p.Id) is not null){p.Message="Arena verwendet normierte Match-Angriffe; PvE-Skills sind hier gesperrt.";return true;}
        var skill=Catalog.Classes[p.Data.ClassId].Skills.FirstOrDefault(s=>s.Id==id&&s.Enabled&&p.Level>=s.MinLevel);
        if(skill is null||p.Data.State.Focus<skill.FocusCost||p.Mana<skill.Cost||p.Cooldowns.GetValueOrDefault(id)>time){p.Message="Skill noch nicht freigeschaltet, Mana fehlt oder Cooldown aktiv.";return true;}
        var needsTarget=skill.Kind is "damage" or "dot" or "slow" or "break" or "drain" or "taunt";
        monsters.TryGetValue(p.Target,out var enemy);
        if(needsTarget&&(enemy is null||enemy.Hp<=0||Distance(p.X,p.Z,enemy.X,enemy.Z)>skill.Range)){p.Message="Lebendes Ziel außerhalb der Skill-Reichweite.";return true;}
        if(skill.Kind=="explode"&&!summons.ContainsKey(p.Id)){p.Message="Zuerst eine eigene Kreatur beschwören.";return true;}
        GearRules.Use(p.Data.State);p.Data.State.Focus-=skill.FocusCost;p.Mana-=skill.Cost;p.Cooldowns[id]=time+skill.Cooldown;p.SkillSerial++;p.AttackSerial++;CharacterRules.Progress(p.Data.State,"skill",id,Catalog);
        switch(skill.Kind){
            case "heal":p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+(int)skill.Power+ClassRules.Healing(p.Data,Catalog)+p.Data.State.Attributes.GetValueOrDefault("wisdom"));break;
            case "party_heal":foreach(var ally in FriendlyTargets(p,15))ally.Hp=Math.Min(CharacterRules.MaxHp(ally.Data,Catalog),ally.Hp+(int)skill.Power+ClassRules.Healing(p.Data,Catalog)+p.Data.State.Attributes.GetValueOrDefault("wisdom"));break;
            case "buff":p.BuffUntil=time+skill.Duration;break;
            case "party_buff":foreach(var ally in FriendlyTargets(p,15))ally.BuffUntil=time+skill.Duration;break;
            case "hot":p.Cooldowns["status:hot"]=time+skill.Duration;break;
            case "haste":p.Cooldowns["status:haste"]=time+skill.Duration;break;
            case "cleanse":foreach(var ally in FriendlyTargets(p,15))ally.Cooldowns.Remove("status:weaken");break;
            case "blink":for(var i=0;i<16;i++)Move(p,Math.Sin(p.Yaw)*Catalog.BlinkDistance/16*(skill.Status=="back"?1:-1),Math.Cos(p.Yaw)*Catalog.BlinkDistance/16*(skill.Status=="back"?1:-1));break;
            case "summon":summons[p.Id]=new Summon(p.Id,skill.Status,p.X-1,p.Z+1,time+skill.Duration);break;
            case "aoe":case "explode":
                var cx=p.X;var cz=p.Z;
                if(skill.Kind=="explode"){cx=summons[p.Id].X;cz=summons[p.Id].Z;summons.Remove(p.Id);}
                else if(enemy is not null&&enemy.Hp>0&&Distance(p.X,p.Z,enemy.X,enemy.Z)<=skill.Range){cx=enemy.X;cz=enemy.Z;}
                foreach(var m in monsters.Values.Where(m=>m.Hp>0&&Distance(cx,cz,m.X,m.Z)<=skill.Radius).ToArray())HitMonster(p,m,(int)(Damage(p)*skill.Power));
                break;
            default:
                var before=enemy!.Hp;HitMonster(p,enemy,(int)(Damage(p)*skill.Power));
                if(skill.Kind=="drain")p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+(before-enemy.Hp)/2);
                if(skill.Kind=="dot"){enemy.DotOwner=p.Id;enemy.DotUntil=time+skill.Duration;enemy.DotPower=Math.Max(1,Damage(p)/3);enemy.NextDot=time+1;}
                if(skill.Kind=="slow")enemy.SlowUntil=time+skill.Duration;
                if(skill.Kind=="break")enemy.BreakUntil=time+skill.Duration;
                if(skill.Kind=="taunt"){enemy.ForcedTarget=p.Id;enemy.TauntUntil=time+skill.Duration;enemy.HardTauntUntil=enemy.TauntUntil;}
                break;
        }
        p.Message=skill.Name+" eingesetzt.";return true;
    }
    private void GrantPetExp(Player p){var pet=p.Data.State.Pet;if(!pet.Active)return;pet.Exp+=R.KillExp;while(pet.Exp>=pet.Level*Catalog.PetExpPerLevel){pet.Exp-=pet.Level*Catalog.PetExpPerLevel;pet.Level++;}}
    private void TickPet(Player p,double dt)
    {
        if((p.Data.State.Pet.TrainingUntil>0&&p.Data.State.Pet.TrainingUntil<=DateTimeOffset.UtcNow.ToUnixTimeSeconds())||p.Data.State.OwnedPets.Any(pet=>pet.TrainingUntil>0&&pet.TrainingUntil<=DateTimeOffset.UtcNow.ToUnixTimeSeconds()))PetRules.CompleteAll(p.Data,DateTimeOffset.UtcNow.ToUnixTimeSeconds(),Catalog);
        var activePet=p.Data.State.Pet;
        if(activePet.Hp<=0){if(activePet.RecoveryUntil<=DateTimeOffset.UtcNow.ToUnixTimeSeconds())activePet.Hp=PetRules.MaxHp(activePet);else return;}
        if(!activePet.Active||activePet.TrainingUntil>0)return;
        var distance=Distance(p.PetX,p.PetZ,p.X+1,p.Z+1);if(distance>15){p.PetX=p.X+1;p.PetZ=p.Z+1;}else if(distance>.1){p.PetX+=(p.X+1-p.PetX)*Math.Min(1,dt*5);p.PetZ+=(p.Z+1-p.PetZ)*Math.Min(1,dt*5);}
        if(time<p.NextPet)return;p.NextPet=time+Catalog.PetInterval;var pet=p.Data.State.Pet;
        if(pet.Mode=="heal")p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+Catalog.PetHeal+pet.Level+pet.Traits.GetValueOrDefault("affinity",1)+pet.Evolution*2);
        else if(pet.Mode=="buff")p.BuffUntil=Math.Max(p.BuffUntil,time+4);
        else if(pet.Mode=="gather"){
            var node=Catalog.Nodes.FirstOrDefault(n=>1+p.Data.State.Professions.GetValueOrDefault(n.Profession)/Catalog.ProfessionLevelXp>=n.MinProfessionLevel&&Distance(p.X,p.Z,n.X,n.Z)<=5&&p.Cooldowns.GetValueOrDefault("gather:"+n.Id)<=time);
            if(node is not null&&PetRules.AddBag(pet,node.Item,Catalog)){p.Cooldowns["gather:"+node.Id]=time+5;p.Data.State.Professions[node.Profession]=p.Data.State.Professions.GetValueOrDefault(node.Profession)+Catalog.ProfessionGatherXp;}
        }
        else if(pet.Mode=="loot"){
            var drop=loot.Values.FirstOrDefault(d=>d.Owner==p.Id&&Distance(p.X,p.Z,d.X,d.Z)<=5);
            if(drop is not null&&CharacterRules.Add(p.Data.State,DropItem(drop),1,Catalog)){loot.Remove(drop.Id);p.Coins+=R.LootCoins;p.Scrap+=R.LootScrap;}
        }else if(p.Target!=""&&monsters.TryGetValue(p.Target,out var m)&&m.Hp>1&&Distance(p.X,p.Z,m.X,m.Z)<=Catalog.PetRange){var damage=Math.Min(m.Hp-1,Catalog.PetDamage+pet.Level+pet.Traits.GetValueOrDefault("strength",1)+pet.Evolution*2);m.Hp-=damage;m.HurtSerial++;m.LastDamage=damage;}
    }
    private object CharacterView(Player p)
    {
        var c=CharacterRules.Clone(Export(p)).State;
        return new{commitReceipt=p.Data.LastCommit,savedTick=p.SavedTick,currentTick=tick,region=RegionName(p),id=p.Id,name=p.Data.Name,classId=p.Data.ClassId,prestige=c.Prestige,appearance=p.Data.Appearance,gear=c.Gear,equippedGear=c.EquippedGear,attributes=c.Attributes,inventory=c.Inventory,equipment=c.Equipment,professions=c.Professions,quests=Catalog.Quests.Select(q=>new{q.Id,q.Name,q.Type,q.Required,navigation=CharacterRules.Navigation(c,q,Catalog),progress=c.QuestProgress.GetValueOrDefault(q.Id),claimed=c.ClaimedQuests.Contains(q.Id)&&(!q.Daily||c.DailyClaims.GetValueOrDefault(q.Id)==CharacterRules.Day),unlocked=CharacterRules.QuestOpen(c,q)&&(q.ClassId==""||q.ClassId==p.Data.ClassId)}).ToArray(),
            focus=c.Focus,resource=Catalog.Classes[p.Data.ClassId].Resource,passive=Catalog.Classes[p.Data.ClassId].Passive,passiveActive=p.Level>=Catalog.Classes[p.Data.ClassId].PassiveLevel,mana=p.Mana,nextLevelExp=c.Level*Catalog.LevelExpBase,maxMana=Catalog.BaseMana+c.Attributes.GetValueOrDefault("wisdom")*Catalog.WisdomMana,attack=Damage(p),defense=CharacterRules.Defense(p.Data,Catalog),farmXp=c.FarmXp,farmCost=CharacterRules.FarmCost(c,Catalog).ToString(),pet=c.Pet,ownedPets=c.OwnedPets.ToArray(),persistent=p.Data.AccountId!=""&&Store.Durable,
            skillbook=Catalog.Classes[p.Data.ClassId].Skills.Select(s=>SkillView(p,s)).ToArray(),automation=FarmView(p),skills=c.SkillBar.Take(6).Select(id=>Catalog.Classes[p.Data.ClassId].Skills.FirstOrDefault(s=>s.Id==id)??Catalog.Classes[p.Data.ClassId].Skills[0]).Select(s=>SkillView(p,s)).ToArray()};
    }
}
