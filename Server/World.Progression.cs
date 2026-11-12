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
        lock(gate){if(players.Count>=100||players.ContainsKey(record.Id)||pendingSaves.ContainsKey(record.Id))return null;
            if(record.AccountId!=""){var latest=Store.Get(record.Id);if(latest is null||latest.AccountId!=record.AccountId)return null;record=latest;}
            var p=new Player(record.Id,record.State.X,record.State.Z,100);Import(p,record);
            if(!district.Walkable(p.X,p.Z)){p.X=district.Spawn.X;p.Z=district.Spawn.Z;}
            p.Hp=Math.Clamp(record.State.Hp,0,CharacterRules.MaxHp(record,Catalog));p.PetX=p.X+1;p.PetZ=p.Z+1;players[p.Id]=p;return p.Id;}
    }
    private CharacterRecord Export(Player p)
    {
        var c=p.Data.State;c.Level=p.Level;c.Exp=p.Exp;c.StatPoints=p.StatPoints;c.Coins=p.Coins;c.Scrap=p.Scrap;c.Kills=p.Kills;c.Quest=p.Quest;c.X=p.X;c.Z=p.Z;c.Mana=p.Mana;c.Hp=p.Hp;c.Timers=p.Cooldowns.ToDictionary(k=>k.Key,k=>wallStart+k.Value);c.Timers["pet"]=wallStart+p.NextPet;c.Timers["respawn"]=wallStart+p.Respawn;c.BuffExpires=wallStart+p.BuffUntil;
        return p.Data;
    }
    private void Import(Player p,CharacterRecord d)
    {p.Data=d;var c=d.State;p.X=c.X;p.Z=c.Z;p.Level=c.Level;p.Exp=c.Exp;p.StatPoints=c.StatPoints;p.Coins=c.Coins;p.Scrap=c.Scrap;p.Kills=c.Kills;p.Quest=c.Quest;p.Mana=c.Mana;p.Hp=c.Hp;p.Cooldowns=c.Timers.ToDictionary(k=>k.Key,k=>k.Value-wallStart);p.NextPet=c.Timers.GetValueOrDefault("pet")-wallStart;p.Respawn=c.Timers.GetValueOrDefault("respawn")-wallStart;p.BuffUntil=c.BuffExpires-wallStart;}
    private int Damage(Player p)=>CharacterRules.Attack(Export(p),Catalog)+(p.BuffUntil>time?Catalog.BuffAttack:0);
    private void CommitAction(Player p,CharacterRecord previous,int oldHp,Dictionary<string,Drop> oldLoot,Dictionary<string,Monster> oldMonsters)
    {
        if(p.Data.AccountId=="")return;
        try{Store.Save(Export(p));}catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){Import(p,previous);p.Hp=oldHp;loot.Clear();foreach(var item in oldLoot)loot[item.Key]=item.Value;monsters.Clear();foreach(var item in oldMonsters)monsters[item.Key]=item.Value;p.Message="Speichern fehlgeschlagen; Aktion zurückgesetzt. Bitte erneut versuchen.";}
    }
    private void Checkpoint()
    {
        foreach(var p in players.Values.Where(p=>p.Data.AccountId!=""))try{Store.Save(Export(p));}catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){p.Message="Datenbank nicht erreichbar; letzter Checkpoint noch ausstehend.";}
        foreach(var pair in pendingSaves.ToArray())try{Store.Save(pair.Value);pendingSaves.Remove(pair.Key);}catch(Exception e)when(e is GameRuleException or Npgsql.NpgsqlException){}
    }
    private bool Near(Player p,string service)=>Catalog.Services.TryGetValue(service,out var point)&&Distance(p.X,p.Z,point.X,point.Z)<=3;
    private bool ExtendedAction(Player p,string kind,string target)
    {
        var c=Export(p).State;var ok=false;
        switch(kind){
            case "stat":ok=CharacterRules.Allocate(c,target,Catalog);break;
            case "farm_stat":ok=CharacterRules.FarmPoint(c,Catalog);break;
            case "buy":ok=Near(p,"merchant")&&CharacterRules.Buy(c,target,Catalog);break;
            case "sell":ok=Near(p,"merchant")&&CharacterRules.Sell(c,target,Catalog);break;
            case "equip":ok=CharacterRules.Equip(c,target,Catalog);break;
            case "craft":ok=Near(p,"workbench")&&CharacterRules.Craft(c,target,Catalog);if(ok)CharacterRules.Progress(c,"craft",target,Catalog);break;
            case "gather":
                var node=Catalog.Nodes.FirstOrDefault(n=>n.Id==target);
                if(node is not null&&Distance(p.X,p.Z,node.X,node.Z)<=3&&p.Cooldowns.GetValueOrDefault("gather:"+target)<=time&&CharacterRules.Add(c,node.Item,1,Catalog)){
                    p.Cooldowns["gather:"+target]=time+5;c.Professions[node.Profession]=c.Professions.GetValueOrDefault(node.Profession)+1;c.FarmXp=(BigInteger.Parse(c.FarmXp)+10).ToString();CharacterRules.Progress(c,"gather",target,Catalog);ok=true;
                }break;
            case "claim":ok=Near(p,"merchant")&&CharacterRules.Claim(c,target,Catalog);break;
            case "use":if(target=="potion"&&c.Inventory.GetValueOrDefault(target)>0&&p.Hp<CharacterRules.MaxHp(p.Data,Catalog)&&p.Cooldowns.GetValueOrDefault("potion")<=time){c.Inventory[target]--;if(c.Inventory[target]==0)c.Inventory.Remove(target);p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+Catalog.Items[target].Heal);p.Cooldowns["potion"]=time+3;ok=true;}break;
            case "pet":if(target is "combat" or "heal" or "loot"){c.Pet.Active=true;c.Pet.Mode=target;p.PetX=p.X+1;p.PetZ=p.Z+1;ok=true;}else if(target=="off"){c.Pet.Active=false;ok=true;}break;
            case "teleport":if(Near(p,"teleporter")||Near(p,"return")){p.X=district.Spawn.X;p.Z=p.Z<30?Catalog.Services["return"].Z:district.Spawn.Z;ok=true;}break;
            case "skill":return Cast(p,target);
            default:return false;
        }
        if(ok){var px=p.X;var pz=p.Z;p.Level=c.Level;p.Exp=c.Exp;p.StatPoints=c.StatPoints;p.Coins=c.Coins;p.Scrap=c.Scrap;p.X=px;p.Z=pz;p.Message="Aktion erfolgreich: "+kind;}else p.Message="Nicht möglich: Reichweite, Voraussetzungen, Material, Platz oder Cooldown prüfen.";
        return true;
    }
    private bool Cast(Player p,string id)
    {
        var skill=Catalog.Classes[p.Data.ClassId].Skills.FirstOrDefault(s=>s.Id==id&&s.Enabled);
        if(skill is null||p.Mana<skill.Cost||p.Cooldowns.GetValueOrDefault(id)>time){p.Message="Skill gesperrt, Mana fehlt oder Cooldown aktiv.";return true;}
        if(skill.Kind=="damage"&&(!monsters.TryGetValue(p.Target,out var enemy)||enemy.Hp<=0||Distance(p.X,p.Z,enemy.X,enemy.Z)>skill.Range)){p.Message="Lebendes Ziel außerhalb der Skill-Reichweite.";return true;}
        p.Mana-=skill.Cost;p.Cooldowns[id]=time+skill.Cooldown;p.SkillSerial++;p.AttackSerial++;CharacterRules.Progress(p.Data.State,"skill",id,Catalog);
        if(skill.Kind=="heal")p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+(int)skill.Power+p.Data.State.Attributes.GetValueOrDefault("wisdom"));
        else if(skill.Kind=="buff")p.BuffUntil=time+Catalog.BuffSeconds;
        else {
            var m=monsters[p.Target];var damage=Math.Max(1,(int)(Damage(p)*skill.Power));m.Hp=Math.Max(0,m.Hp-damage);m.HurtSerial++;m.LastDamage=damage;
            if(m.Hp==0){m.Respawn=time+R.MonsterRespawnSeconds;p.Attacking=false;GrantExp(p,R.KillExp);if(p.Quest=="active"){p.Kills++;if(p.Kills>=R.QuestKills)p.Quest="ready";}var drop=Guid.NewGuid().ToString("N");loot[drop]=new(drop,p.Id,m.Spawn.Id,m.X,m.Z,time+90);GrantPetExp(p);}
        }
        p.Message=skill.Name+" eingesetzt.";return true;
    }
    private void GrantPetExp(Player p){var pet=p.Data.State.Pet;if(!pet.Active)return;pet.Exp+=R.KillExp;while(pet.Exp>=pet.Level*Catalog.PetExpPerLevel){pet.Exp-=pet.Level*Catalog.PetExpPerLevel;pet.Level++;}}
    private void TickPet(Player p,double dt)
    {
        if(!p.Data.State.Pet.Active)return;
        var distance=Distance(p.PetX,p.PetZ,p.X+1,p.Z+1);if(distance>15){p.PetX=p.X+1;p.PetZ=p.Z+1;}else if(distance>.1){p.PetX+=(p.X+1-p.PetX)*Math.Min(1,dt*5);p.PetZ+=(p.Z+1-p.PetZ)*Math.Min(1,dt*5);}
        if(time<p.NextPet)return;p.NextPet=time+Catalog.PetInterval;var pet=p.Data.State.Pet;
        if(pet.Mode=="heal")p.Hp=Math.Min(CharacterRules.MaxHp(p.Data,Catalog),p.Hp+Catalog.PetHeal+pet.Level);
        else if(pet.Mode=="loot"){
            var drop=loot.Values.FirstOrDefault(d=>d.Owner==p.Id&&Distance(p.X,p.Z,d.X,d.Z)<=5);
            if(drop is not null&&CharacterRules.Add(p.Data.State,"scrap",1,Catalog)){loot.Remove(drop.Id);p.Coins+=R.LootCoins;p.Scrap+=R.LootScrap;}
        }else if(p.Target!=""&&monsters.TryGetValue(p.Target,out var m)&&m.Hp>1&&Distance(p.X,p.Z,m.X,m.Z)<=Catalog.PetRange){var damage=Math.Min(m.Hp-1,Catalog.PetDamage+pet.Level);m.Hp-=damage;m.HurtSerial++;m.LastDamage=damage;}
    }
    private object CharacterView(Player p)
    {
        var c=CharacterRules.Clone(Export(p)).State;
        return new{id=p.Id,name=p.Data.Name,classId=p.Data.ClassId,attributes=c.Attributes,inventory=c.Inventory,equipment=c.Equipment,professions=c.Professions,quests=Catalog.Quests.Select(q=>new{q.Id,q.Name,q.Type,q.Required,progress=c.QuestProgress.GetValueOrDefault(q.Id),claimed=c.ClaimedQuests.Contains(q.Id),unlocked=q.Prerequisite==""||c.ClaimedQuests.Contains(q.Prerequisite)}).ToArray(),
            mana=p.Mana,nextLevelExp=c.Level*Catalog.LevelExpBase,maxMana=Catalog.BaseMana+c.Attributes.GetValueOrDefault("wisdom")*Catalog.WisdomMana,attack=Damage(p),defense=CharacterRules.Defense(p.Data,Catalog),farmXp=c.FarmXp,farmCost=CharacterRules.FarmCost(c,Catalog).ToString(),pet=c.Pet,persistent=p.Data.AccountId!=""&&Store.Durable,
            skills=Catalog.Classes[p.Data.ClassId].Skills.Select(s=>new{s.Id,s.Name,s.Kind,s.Enabled,s.Cost,cooldown=Math.Max(0,p.Cooldowns.GetValueOrDefault(s.Id)-time)}).ToArray()};
    }
}
