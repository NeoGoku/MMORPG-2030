extends RefCounted

var sim
var catalog: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://data/game.json"))
var class_id := "Shield Sword"
var nickname := "Reisender"
var focus:=100.0
var mana := 100.0
var attributes: Dictionary = {}
var inventory: Dictionary = {"potion": 2}
var equipment: Dictionary = {}
var professions: Dictionary = {}
var quest_progress: Dictionary = {}
var claimed: Array = []
var prestige := 0
var daily_claims: Dictionary={}
var farm_xp := 0
var farmed := 0
var cooldowns: Dictionary = {}
var skill_bar: Array = ["skill1","skill2","skill3","skill4","skill5","skill6"]
var summons: Array = []
var effects: Dictionary = {}
var buff := 0.0
var pet: Dictionary = {"active":false,"mode":"combat","level":1,"exp":0,"evolution":0,"parents":[]}
var automation: Dictionary={"active":false,"returning":false,"remaining":0.0,"radius":0.0,"homeX":0.0,"homeZ":0.0,"path":[],"idle":0.0,"stuck":0.0,"last":Vector2.ZERO}
var owned_pets: Array=[]
var pet_position := Vector2.ZERO
var pet_timer := 0.0

func _init(owner_sim) -> void:
    pet.merge({"id":"local-pet","hp":100,"recoveryUntil":0.0,"trainingUntil":0.0,"traits":{"strength":1,"affinity":1,"vitality":1},"inventory":{}},true)
    sim = owner_sim
    mana = catalog.baseMana
    for key in catalog.attributes:
        attributes[key] = 0
    for key in catalog.professions:
        professions[key] = 0

func attack() -> int:
    var passive: int=int(catalog.classes[class_id].get("passiveAttack",0)) if sim.player.level>=catalog.classes[class_id].get("passiveLevel",5) else 0
    var value: int = passive+int(catalog.baseAttack) + mini(prestige,100) + int(attributes[catalog.classes[class_id].attribute]) + (int(catalog.buffAttack) if buff > 0 else 0) - (int(catalog.statusPenalty) if effects.get("weaken",0)>0 else 0)
    for item in equipment.values():
        value += int(catalog.items[item].get("attack",0))
    return value

func defense() -> int:
    var value: int = attributes.armor+(int(catalog.classes[class_id].get("passiveDefense",0)) if sim.player.level>=5 else 0)
    for item in equipment.values():
        value += int(catalog.items[item].get("defense",0))
    return value

func slots() -> int:
    var count := 0
    for item in inventory:
        count += ceili(float(inventory[item])/catalog.items[item].stack)
    return count

func add(item: String, count: int) -> bool:
    var previous: int = inventory.get(item,0)
    inventory[item] = previous + count
    if slots() > catalog.inventorySlots:
        if previous == 0: inventory.erase(item)
        else: inventory[item] = previous
        return false
    return true

func progress(kind: String, item: String) -> void:
    for q in catalog.quests:
        if sim.player.level<q.get("minLevel",1):continue
        if q.get("daily",false) and daily_claims.get(q.id,-1)!=int(Time.get_unix_time_from_system()/86400):
            claimed.erase(q.id);quest_progress[q.id]=0;daily_claims[q.id]=int(Time.get_unix_time_from_system()/86400)
        if q.event == kind and (q.item == "" or q.item == item) and not claimed.has(q.id) and (q.prerequisite == "" or claimed.has(q.prerequisite)):
            quest_progress[q.id] = mini(q.required,int(quest_progress.get(q.id,0))+1)

func near(service: String) -> bool:
    var points: Array=[]
    if catalog.services.has(service):points.append(catalog.services[service])
    for outpost in catalog.get("outposts",[]):
        if outpost.service==service:points.append(outpost)
    for point in points:
        if Vector2(sim.player.x-point.x,sim.player.z-point.z).length()<=3:return true
    return false

func action(kind: String, target: String) -> bool:
    var p: Dictionary = sim.player
    sync_owned()
    if kind in ["target","attack","skill","teleport"] and automation.active:automation.active=false;p.attacking=false
    if p.hp <= 0: return true
    var ok := false
    match kind:
        "reset_stats":
            if p.level>=catalog.resetLevel and inventory.get("reset_stone",0)>0:
                for key in attributes:p.statPoints+=attributes[key];attributes[key]=0
                inventory.reset_stone-=1
                if inventory.reset_stone==0:inventory.erase("reset_stone")
                ok=true
        "prestige":
            if near("merchant") and p.level>=catalog.prestigeLevel and inventory.get("boss_core",0)>0:
                inventory.boss_core-=1
                if inventory.boss_core==0:inventory.erase("boss_core")
                prestige+=1;ok=true
        "farm_start":
            var radius:=int(target) if target.is_valid_int() else 0
            var safe: bool=p.z>=2 and p.hp>=p.maxHp*.6
            for m in sim.monsters:
                if Vector2(p.x-m.homeX,p.z-m.homeZ).length()<12:safe=false
            if safe and radius>0 and radius<=catalog.farmRadius and cooldowns.get("farm:ready",0)<=0:
                automation={"active":true,"returning":false,"realUntil":Time.get_unix_time_from_system()+catalog.farmSeconds,"remaining":float(catalog.farmSeconds),"radius":float(radius),"homeX":p.x,"homeZ":p.z,"path":[Vector2(p.x,p.z)],"idle":0.0,"stuck":0.0,"last":Vector2(p.x,p.z)}
                cooldowns["farm:ready"]=catalog.farmSeconds+30
                ok=true
        "farm_stop":automation.active=false;p.attacking=false;ok=true
        "slot":
            var parts:=target.split("|")
            if parts.size()==2 and parts[0].is_valid_int() and int(parts[0])>=0 and int(parts[0])<6:
                for skill in catalog.classes[class_id].skills:
                    if skill.id==parts[1]:skill_bar[int(parts[0])]=parts[1];ok=true
        "stat":
            if attributes.has(target) and p.statPoints > 0:
                p.statPoints -= 1
                attributes[target] += 1
                p.maxHp = catalog.baseHp + attributes.vitality*catalog.vitalityHp
                ok = true
        "farm_stat":
            var cost := farm_cost()
            if farm_xp >= cost:
                farm_xp -= cost
                farmed += 1
                p.statPoints += 1
                ok = true
        "equip":
            if inventory.get(target,0) > 0 and catalog.items[target].get("slot","") != "":
                equipment[catalog.items[target].slot] = target
                ok = true
        "buy":
            if near("merchant") and catalog.items.has(target) and catalog.items[target].get("vendor",false) and p.coins >= catalog.items[target].price and add(target,1):
                p.coins -= catalog.items[target].price
                ok = true
        "sell":
            if near("merchant") and inventory.get(target,0)>0 and not equipment.values().has(target):
                inventory[target] -= 1
                if inventory[target] == 0: inventory.erase(target)
                p.coins += maxi(1,int(catalog.items[target].price)/2)
                ok = true
        "craft":
            if near("workbench") and catalog.recipes.has(target):
                var recipe: Dictionary = catalog.recipes[target]
                var enough: bool=1+int(professions.get(recipe.profession,0))/int(catalog.professionLevelXp)>=int(recipe.get("minProfessionLevel",1))
                for item in recipe.inputs:
                    if inventory.get(item,0) < recipe.inputs[item]: enough = false
                if enough:
                    var previous := inventory.duplicate()
                    for item in recipe.inputs:
                        inventory[item] -= recipe.inputs[item]
                        if inventory[item] == 0: inventory.erase(item)
                    if add(recipe.output,recipe.count):
                        professions[recipe.profession] += catalog.professionCraftXp
                        farm_xp += 25
                        progress("craft",target)
                        ok = true
                    else: inventory = previous
        "gather":
            for node in catalog.nodes:
                if node.id == target and 1+int(professions.get(node.profession,0))/int(catalog.professionLevelXp)>=int(node.get("minProfessionLevel",1)) and Vector2(p.x-node.x,p.z-node.z).length() <= 3 and cooldowns.get("gather:"+target,0)<=0 and add(node.item,1):
                    professions[node.profession] += catalog.professionGatherXp
                    farm_xp += 10
                    cooldowns["gather:"+target] = 5.0
                    progress("gather",target)
                    ok = true
        "claim":
            for q in catalog.quests:
                if near("merchant") and q.id == target and p.level>=q.get("minLevel",1) and (q.prerequisite=="" or claimed.has(q.prerequisite)) and not claimed.has(target) and quest_progress.get(target,0) >= q.required:
                    var old_inventory: Dictionary=inventory.duplicate()
                    var reward_ok:=true
                    for item in q.get("rewards",{}):
                        if not add(item,q.rewards[item]):reward_ok=false
                    if not reward_ok:inventory=old_inventory;continue
                    p.statPoints+=q.get("points",0)
                    claimed.append(target)
                    p.coins += q.coins
                    sim._exp(q.exp)
                    ok = true
        "use":
            if target=="potion" and inventory.get(target,0)>0 and p.hp<p.maxHp and cooldowns.get("potion",0)<=0:
                inventory[target] -= 1
                if inventory[target]==0: inventory.erase(target)
                p.hp = mini(p.maxHp,p.hp+catalog.items[target].heal)
                cooldowns["potion"] = 3.0
                ok = true
        "pet":
            if target in ["combat","heal","loot","tank","buff","gather"]:
                pet.active = pet.trainingUntil<=0
                pet.mode = target
                pet_position = Vector2(p.x+1,p.z+1)
                ok = true
            elif target=="off":
                pet.active = false
                ok = true
        "pet_hatch":
            if near("workbench") and owned_pets.size()<catalog.maxOwnedPets and consume_item("pet_egg",1):
                var created: Dictionary=pet_template()
                owned_pets.append(created)
                ok=true
        "pet_breed":
            for second in owned_pets:
                if second.id==target and second.id!=pet.id and near("workbench") and pet.level>=catalog.petBreedLevel and second.level>=catalog.petBreedLevel and pet.trainingUntil<=0 and second.trainingUntil<=0 and owned_pets.size()<catalog.maxOwnedPets and consume_item("pet_feed",2):
                    var created: Dictionary=pet_template()
                    created.parents=[pet.id,second.id]
                    for key in created.traits:created.traits[key]=pet.traits[key] if randi_range(0,1)==0 else second.traits[key]
                    owned_pets.append(created)
                    professions.animal_breeding+=catalog.professionCraftXp
                    farm_xp+=25
                    ok=true
                    break
        "pet_evolve":
            if near("workbench") and pet.level>=catalog.petEvolutionLevel and pet.evolution<3 and pet.trainingUntil<=0 and consume_item("evolution_shard",1):pet.evolution+=1;pet.hp=pet_max_hp();ok=true
        "pet_train":
            if near("workbench") and not pet.active and pet.trainingUntil<=0 and consume_item("pet_feed",1):pet.trainingUntil=Time.get_unix_time_from_system()+catalog.petTrainingSeconds;ok=true
        "pet_select":
            for selected in owned_pets:
                if selected.id==target and selected.id!=pet.id:
                    var active: bool=pet.active
                    pet.active=false
                    sync_owned()
                    pet=selected.duplicate(true)
                    pet.active=active and pet.trainingUntil<=0
                    ok=true
                    break
        "pet_take":
            var previous:=inventory.duplicate()
            ok=true
            for item in pet.inventory:
                if not add(item,pet.inventory[item]):ok=false;break
            if ok:pet.inventory.clear()
            else:inventory=previous
        "world_transit":
            for passage in sim.data.get("passages",[]):
                if passage.id!=target:continue
                var destination: Dictionary={}
                if Vector2(p.x-passage.from.x,p.z-passage.from.z).length()<=3 and p.level>=passage.minLevel:destination=passage.to
                elif Vector2(p.x-passage.to.x,p.z-passage.to.z).length()<=3:destination=passage.from
                if not destination.is_empty() and sim._walkable(destination.x,destination.z):
                    p.x=destination.x;p.z=destination.z;p.attacking=false;p.target="";automation.active=false;ok=true
        "transit":
            if Vector2(p.x,p.z).distance_to(Vector2(0,-40))<=3 and p.level>=5 and p.quest=="complete":p.x=0.0;p.z=-59.0;ok=true
            elif Vector2(p.x,p.z).distance_to(Vector2(0,-59))<=3:p.x=0.0;p.z=-40.0;ok=true
        "teleport":
            if near("teleporter") or (catalog.services.has("return") and near("return")):
                p.x = 0.0
                p.z = catalog.services["return"].z if p.z < 30 else sim.data.spawn.z
                ok = true
        "skill":
            return cast(target)
        _:
            return false
    p.message = "Aktion erfolgreich: "+kind if ok else "Reichweite, Voraussetzungen, Material, Platz oder Cooldown prüfen."
    return true

func _hit(enemy: Dictionary, damage: int) -> void:
    if enemy.hp<=0:return
    if enemy.get("boss",false):damage=maxi(1,damage/2)
    if enemy.get("break",0)>0:damage+=catalog.statusPenalty
    enemy.hp=maxi(0,enemy.hp-damage)
    enemy.lastDamage=damage
    enemy.hurtSerial+=1
    enemy.aggro=5.0
    if enemy.hp==0:
        enemy.respawn=sim.time+sim.rules.monsterRespawnSeconds
        sim.player.attacking=false
        sim._exp(sim.rules.killExp*enemy.get("level",1))
        progress("kill",enemy.id)
        if sim.player.quest=="active" and str(enemy.id).begins_with("drone-"):
            sim.player.kills+=1
            if sim.player.kills>=sim.rules.questKills:sim.player.quest="ready"
        sim.drop_sequence+=1
        sim.drops.append({"id":"drop-%d"%sim.drop_sequence,"monsterId":enemy.id,"x":enemy.x,"z":enemy.z,"expires":sim.time+90})
        reward_pet()

func cast(id: String) -> bool:
    var p: Dictionary=sim.player
    for skill in catalog.classes[class_id].skills:
        if skill.id!=id:continue
        if not skill.enabled or p.level<skill.minLevel or focus<skill.get("focusCost",0) or mana<skill.cost or cooldowns.get(id,0)>0:
            p.message="Skill noch nicht freigeschaltet, Mana fehlt oder Cooldown aktiv."
            return true
        var enemy: Dictionary={}
        for m in sim.monsters:
            if m.id==p.target and m.hp>0:enemy=m
        if skill.kind in ["damage","dot","slow","break","drain","taunt"] and (enemy.is_empty() or Vector2(p.x-enemy.x,p.z-enemy.z).length()>skill.range):
            p.message="Lebendes Ziel außerhalb der Skill-Reichweite."
            return true
        if skill.kind=="explode" and summons.is_empty():
            p.message="Zuerst eine Kreatur beschwören."
            return true
        focus-=skill.get("focusCost",0)
        mana-=skill.cost
        cooldowns[id]=skill.cooldown
        p.attackSerial+=1
        p.skillSerial=p.get("skillSerial",0)+1
        progress("skill",id)
        match skill.kind:
            "heal","party_heal":p.hp=mini(p.maxHp,p.hp+int(skill.power)+attributes.wisdom)
            "buff","party_buff":buff=skill.duration
            "hot":effects.hot=skill.duration
            "haste":effects.haste=skill.duration
            "cleanse":effects.erase("weaken")
            "blink":
                for i in range(16):sim._move(p,sin(p.yaw)*catalog.blinkDistance/16*(1 if skill.status=="back" else -1),cos(p.yaw)*catalog.blinkDistance/16*(1 if skill.status=="back" else -1))
            "summon":summons=[{"owner":"local","kind":skill.status,"x":p.x-1,"z":p.z+1,"hp":60,"maxHp":60,"remaining":skill.duration,"next":0.0}]
            "aoe","explode":
                var center:=Vector2(p.x,p.z)
                if skill.kind=="explode":center=Vector2(summons[0].x,summons[0].z);summons.clear()
                elif not enemy.is_empty() and Vector2(p.x-enemy.x,p.z-enemy.z).length()<=skill.range:center=Vector2(enemy.x,enemy.z)
                for m in sim.monsters:
                    if m.hp>0 and Vector2(m.x,m.z).distance_to(center)<=skill.radius:_hit(m,int(attack()*skill.power))
            _:
                var before: int=enemy.hp
                _hit(enemy,int(attack()*skill.power))
                if skill.kind=="drain":p.hp=mini(p.maxHp,p.hp+(before-enemy.hp)/2)
                if skill.kind=="dot":enemy.dot=skill.duration;enemy.dot_tick=1.0
                if skill.kind=="slow":enemy.slow=skill.duration
                if skill.kind=="break":enemy["break"]=skill.duration
                if skill.kind=="taunt":enemy.aggro=skill.duration
        p.message=skill.name+" eingesetzt."
        return true
    return true

func tick_effects(dt: float) -> void:
    for key in effects:effects[key]=maxf(0,effects[key]-dt)
    if sim.tick%20==0 and effects.get("hot",0)>0:sim.player.hp=mini(sim.player.maxHp,sim.player.hp+catalog.hotHeal)
    for enemy in sim.monsters:
        for key in ["dot","slow","break","aggro"]:
            if enemy.has(key):enemy[key]=maxf(0,enemy[key]-dt)
        if enemy.hp>0 and enemy.get("dot",0)>0:
            enemy.dot_tick-=dt
            if enemy.dot_tick<=0:enemy.dot_tick=1.0;_hit(enemy,maxi(1,attack()/3))
    for summon in summons.duplicate():
        summon.remaining-=dt
        summon.next-=dt
        if summon.remaining<=0 or summon.hp<=0 or sim.player.hp<=0:summons.erase(summon);continue
        var destination:=Vector2(sim.player.x-1,sim.player.z+1)
        for m in sim.monsters:
            if m.id==sim.player.target and m.hp>0 and Vector2(m.x-sim.player.x,m.z-sim.player.z).length()<=18:
                destination=Vector2(m.x,m.z)
                if summon.next<=0 and Vector2(summon.x,summon.z).distance_to(destination)<=4:
                    summon.next=2.0
                    if summon.kind=="wisp":sim.player.hp=mini(sim.player.maxHp,sim.player.hp+8)
                    else:_hit(m,9 if summon.kind=="wolf" else 5)
        if summon.kind=="wisp" and summon.next<=0:summon.next=3;sim.player.hp=mini(sim.player.maxHp,sim.player.hp+8)
        var vector: Vector2=destination-Vector2(summon.x,summon.z)
        if vector.length()>1:
            vector=vector.normalized()*dt*5
            sim._move(summon,vector.x,vector.y)

func reward_pet() -> void:
    if pet.active:pet.exp += sim.rules.killExp
    while pet.exp >= pet.level*catalog.petExpPerLevel:
        pet.exp -= pet.level*catalog.petExpPerLevel
        pet.level += 1

func farm_cost() -> int:
    return int(catalog.baseFarmCost*pow(catalog.farmGrowth,farmed))

func tick(dt: float) -> void:
    tick_effects(dt)
    for key in cooldowns: cooldowns[key] = maxf(0,cooldowns[key]-dt)
    buff = maxf(0,buff-dt)
    focus=minf(100,focus+dt*catalog.classes[class_id].get("resourceRegen",2))
    mana = minf(catalog.baseMana+attributes.wisdom*catalog.wisdomMana,mana+dt*catalog.manaRegen)
    if pet.exp >= pet.level*catalog.petExpPerLevel:
        pet.exp -= pet.level*catalog.petExpPerLevel
        pet.level += 1
    complete_training(pet)
    for companion in owned_pets:
        if companion.id!=pet.id:complete_training(companion)
    if pet.hp<=0:
        if pet.recoveryUntil<=Time.get_unix_time_from_system():pet.hp=pet_max_hp()
        else:return
    if not pet.active or sim.player.hp<=0 or pet.trainingUntil>0: return
    pet_position = pet_position.lerp(Vector2(sim.player.x+1,sim.player.z+1),minf(1,dt*5))
    pet_timer -= dt
    if pet_timer > 0: return
    pet_timer = catalog.petInterval
    if pet.mode=="heal":sim.player.hp = mini(sim.player.maxHp,sim.player.hp+catalog.petHeal+pet.level+pet.traits.affinity+pet.evolution*2)
    elif pet.mode=="buff":buff=maxf(buff,4)
    elif pet.mode=="gather":
        for node in catalog.nodes:
            if 1+int(professions.get(node.profession,0))/int(catalog.professionLevelXp)>=int(node.get("minProfessionLevel",1)) and Vector2(sim.player.x-node.x,sim.player.z-node.z).length()<=5 and cooldowns.get("gather:"+node.id,0)<=0:
                var old: int=pet.inventory.get(node.item,0)
                pet.inventory[node.item]=old+1
                var count:=0
                for item in pet.inventory:count+=ceili(float(pet.inventory[item])/catalog.items[item].stack)
                if count<=8:cooldowns["gather:"+node.id]=5;professions[node.profession]+=catalog.professionGatherXp
                elif old>0:pet.inventory[node.item]=old
                else:pet.inventory.erase(node.item)
                break
    elif pet.mode=="loot":
        for drop in sim.drops:
            if Vector2(sim.player.x-drop.x,sim.player.z-drop.z).length()<=5 and add(sim.loot_item(drop.monsterId),1):
                sim.drops.erase(drop)
                sim.player.coins += sim.rules.lootCoins
                sim.player.scrap += 1
                break
    else:
        for m in sim.monsters:
            if m.id==sim.player.target and m.hp>1 and Vector2(sim.player.x-m.x,sim.player.z-m.z).length()<=catalog.petRange:
                var damage := mini(m.hp-1,catalog.petDamage+pet.level+pet.traits.strength+pet.evolution*2)
                m.hp -= damage
                m.lastDamage = damage
                m.hurtSerial += 1

func view() -> Dictionary:
    sync_owned()
    var book: Array=catalog.classes[class_id].skills.duplicate(true)
    for skill in book:
        skill.cooldown=cooldowns.get(skill.id,0)
        skill.enabled=skill.enabled and sim.player.level>=skill.minLevel
    var skills: Array=[]
    for id in skill_bar:
        for skill in book:
            if skill.id==id:skills.append(skill)
    var quests: Array = catalog.quests.duplicate(true)
    for q in quests:
        q.progress = quest_progress.get(q.id,0)
        q.navigation=q.get("navigation",{}).duplicate()
        if q.progress>=q.required:
            var traders: Array=[catalog.services.merchant]
            for outpost in catalog.get("outposts",[]):
                if outpost.service=="merchant":traders.append(outpost)
            traders.sort_custom(func(a: Dictionary,b: Dictionary):return Vector2(a.x-sim.player.x,a.z-sim.player.z).length_squared()<Vector2(b.x-sim.player.x,b.z-sim.player.z).length_squared())
            q.navigation=traders[0].duplicate()
        q.claimed = claimed.has(q.id)
        q.unlocked = q.prerequisite=="" or claimed.has(q.prerequisite)
    return {"id":"local","name":nickname,"classId":class_id,"prestige":prestige,"attributes":attributes.duplicate(),"inventory":inventory.duplicate(),"equipment":equipment.duplicate(),"professions":professions.duplicate(),"quests":quests,"focus":int(focus),"resource":catalog.classes[class_id].get("resource","Focus"),"passive":catalog.classes[class_id].get("passive",""),"passiveActive":sim.player.level>=5,"mana":int(mana),"nextLevelExp":sim.player.level*catalog.levelExpBase,"maxMana":catalog.baseMana+attributes.wisdom*catalog.wisdomMana,"attack":attack(),"defense":defense(),"farmXp":str(farm_xp),"farmCost":str(farm_cost()),"automation":automation.duplicate(true),"pet":pet.duplicate(true),"ownedPets":owned_pets.duplicate(true),"persistent":false,"skills":skills,"skillbook":book}

func pets() -> Array:
    return [{"owner":"local","x":pet_position.x,"z":pet_position.y,"level":pet.level,"mode":pet.mode,"hp":pet.hp,"evolution":pet.evolution}] if pet.active else []

func pet_template() -> Dictionary:
    return {"id":"pet-"+str(Time.get_ticks_usec()),"active":false,"mode":"combat","level":1,"exp":0,"evolution":0,"parents":[],"hp":100,"recoveryUntil":0.0,"trainingUntil":0.0,"traits":{"strength":randi_range(1,4),"affinity":randi_range(1,4),"vitality":randi_range(1,4)},"inventory":{}}
func sync_owned() -> void:
    var found:=false
    for i in range(owned_pets.size()):
        if owned_pets[i].id==pet.id:owned_pets[i]=pet.duplicate(true);found=true
        else:owned_pets[i].active=false
    if not found:owned_pets.append(pet.duplicate(true))
func consume_item(item: String,count: int) -> bool:
    if inventory.get(item,0)<count:return false
    inventory[item]-=count
    if inventory[item]==0:inventory.erase(item)
    return true
func pet_max_hp() -> int:return 100+pet.level*5+pet.evolution*20+pet.traits.vitality*5
func complete_training(companion: Dictionary) -> void:
    if companion.trainingUntil<=0 or companion.trainingUntil>Time.get_unix_time_from_system():return
    companion.trainingUntil=0
    companion.exp+=catalog.petTrainingExp
    while companion.exp>=companion.level*catalog.petExpPerLevel:companion.exp-=companion.level*catalog.petExpPerLevel;companion.level+=1

func farm_direction(dt: float,input: Vector2) -> Vector2:
    if not automation.active:return input
    var p: Dictionary=sim.player
    if p.hp<=0:automation.active=false;return Vector2.ZERO
    if input.length()>.01:automation.active=false;p.attacking=false;return input
    automation.remaining-=dt
    if p.hp<p.maxHp*.55 and cooldowns.get("potion",0)<=0 and consume_item("potion",1):p.hp=mini(p.maxHp,p.hp+catalog.items.potion.heal);cooldowns.potion=3.0
    if Time.get_unix_time_from_system()>=automation.get("realUntil",INF) or automation.remaining<=0 or p.hp<p.maxHp*.35 or mana<5 or slots()>=catalog.inventorySlots:automation.returning=true
    var home:=Vector2(automation.homeX,automation.homeZ)
    var pos:=Vector2(p.x,p.z)
    if pos.distance_to(home)>automation.radius+1:automation.returning=true
    if automation.returning:
        p.attacking=false;p.target=""
        while not automation.path.is_empty() and pos.distance_to(automation.path.back())<.6:automation.path.pop_back()
        if automation.path.is_empty():automation.active=false;p.message="Automatik beendet; zurück am Startpunkt.";return Vector2.ZERO
        return farm_steer(automation.path.back(),dt)
    if automation.path.size()<256 and (automation.path.is_empty() or pos.distance_to(automation.path.back())>1):automation.path.append(pos)
    for drop in sim.drops:
        if Vector2(drop.x,drop.z).distance_to(home)<=automation.radius:
            p.attacking=false
            if pos.distance_to(Vector2(drop.x,drop.z))<=3:
                if add(sim.loot_item(drop.monsterId),1):sim.drops.erase(drop);p.coins+=sim.rules.lootCoins;p.scrap+=1
                else:automation.returning=true
                return Vector2.ZERO
            return farm_steer(Vector2(drop.x,drop.z),dt)
    var candidate: Dictionary={}
    var closest:=INF
    for enemy in sim.monsters:
        var distance:=pos.distance_to(Vector2(enemy.x,enemy.z))
        if enemy.hp>0 and Vector2(enemy.x,enemy.z).distance_to(home)<=automation.radius and distance<closest:candidate=enemy;closest=distance
    if candidate.is_empty():automation.idle+=dt;p.attacking=false;automation.returning=automation.idle>15;return Vector2.ZERO
    automation.idle=0
    p.target=candidate.id
    if closest>sim.rules.attackRange:p.attacking=false;return farm_steer(Vector2(candidate.x,candidate.z),dt)
    p.attacking=true
    for skill in catalog.classes[class_id].skills:
        if skill.kind=="damage" and skill.minLevel<=p.level and mana>=skill.cost and cooldowns.get(skill.id,0)<=0:cast(skill.id);break
    return Vector2.ZERO
func farm_steer(goal: Vector2,dt: float) -> Vector2:
    var pos:=Vector2(sim.player.x,sim.player.z)
    if pos.distance_to(automation.last)<.01:automation.stuck+=dt
    else:automation.stuck=0
    automation.last=pos
    if automation.stuck>3:
        if automation.returning:automation.active=false;sim.player.message="Automatik gestoppt: Rückweg blockiert."
        else:automation.returning=true;automation.stuck=0
        return Vector2.ZERO
    var vector: Vector2=(goal-pos).normalized()
    for angle in [0.0,PI/4,-PI/4,PI/2,-PI/2]:
        var direction:=vector.rotated(angle)
        if sim._walkable(pos.x+direction.x*sim.rules.speed*dt,pos.y+direction.y*sim.rules.speed*dt):return direction
    return Vector2.ZERO
