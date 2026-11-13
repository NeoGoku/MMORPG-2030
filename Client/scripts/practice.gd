extends Node

# Explicit local training; no simulated remote players. Uses the server's district/rules.
const Progression = preload("res://scripts/progression.gd")
var m3
var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://data/district.json"))
var rules: Dictionary = data.rules
var player: Dictionary
var monsters: Array = []
var drops: Array = []
var time := 0.0
var tick := 0
var drop_sequence := 0

func _init() -> void:
    player = {"id": "local", "x": data.spawn.x, "z": data.spawn.z, "yaw": 0.0,
        "hp": rules.playerHp, "maxHp": rules.playerHp, "level": 1, "exp": 0, "statPoints": 0,
        "coins": 0, "scrap": 0, "quest": "available", "kills": 0, "target": "", "attacking": false,
        "attackSerial": 0, "hurtSerial": 0, "lastDamage": 0, "nextAttack": 0.0, "respawn": 0.0,
        "message": "Sprich mit Mira bei der Stadtwache. Geh zum goldenen Zeichen."}
    for spawn in data.monsters+data.get("encounters",[]):
        monsters.append({"id": spawn.id, "name": spawn.name, "x": spawn.x, "z": spawn.z,
            "homeX": spawn.x, "homeZ": spawn.z,"reach":spawn.get("reach",2),"speed":spawn.get("speed",1.8),"damageType":spawn.get("damageType","physical"), "family":spawn.get("family",""),"level":spawn.get("level",1),"boss":spawn.get("boss",false),"warning":0.0,"warningX":0.0,"warningZ":0.0,"nextWave":0.0,"hp": spawn.get("hp",rules.monsterHp), "maxHp": spawn.get("hp",rules.monsterHp),
            "attackSerial": 0, "hurtSerial": 0, "lastDamage": 0, "nextAttack": 0.0, "respawn": 0.0})

    m3 = Progression.new(self)
    player.hp = m3.catalog.baseHp
    player.maxHp = m3.catalog.baseHp

func _distance(a: Dictionary, b: Dictionary) -> float:
    return Vector2(a.x - b.x, a.z - b.z).length()

func _walkable(x: float, z: float) -> bool:
    if absf(x) > data.bounds - .45 or absf(z) > data.bounds - .45:
        return false
    for region in data.get("regions",[]):
        if Vector2(x-region.x,z-region.z).length()<region.radius and player.level<region.minLevel:return false
    for b in data.buildings+data.get("walls",[]):
        if absf(x - b.x) < b.width / 2.0 + .45 and absf(z - b.z) < b.depth / 2.0 + .45:
            return false
    return true

func _move(entity: Dictionary, dx: float, dz: float) -> void:
    if _walkable(entity.x + dx, entity.z):
        entity.x += dx
    if _walkable(entity.x, entity.z + dz):
        entity.z += dz

func _exp(amount: int) -> void:
    player.exp += amount
    while player.level < 100 and player.exp >= player.level * m3.catalog.levelExpBase:
        player.exp -= player.level * m3.catalog.levelExpBase
        player.level += 1
        player.statPoints += 1

func act(kind: String, target: String = "") -> void:
    if player.hp <= 0:
        return
    if m3.action(kind,target): return
    match kind:
        "target":
            player.target = ""
            player.attacking = false
            for m in monsters:
                if m.id == target and m.hp > 0 and _distance(player, m) <= 24:
                    player.target = target
        "attack":
            player.attacking = not player.attacking and player.target != ""
            player.message = "Auto-Angriff aktiv. Stehen bleiben und in Reichweite gehen." if player.attacking else "Auto-Angriff beendet."
        "interact":
            if _distance(player, data.npc) > 3:
                player.message = "Mira ist zu weit entfernt. Folge dem goldenen Zeichen."
            elif player.quest == "available":
                player.quest = "active"
                player.message = "Mira: Zerstöre 3 defekte Drohnen im Nordhof. Komm danach zurück."
            elif player.quest == "ready":
                player.quest = "complete"
                player.coins += rules.questCoins
                _exp(int(rules.questExp))
                player.hp = player.maxHp
                player.message = "Mira: Nordtor ist sicherer. Danke! +60 EXP, +25 Münzen."
            else:
                player.message = "Mira: Gute Arbeit!" if player.quest == "complete" else "Mira: Noch %d Drohnen im Nordhof." % (rules.questKills - player.kills)
        "loot":
            for drop in drops:
                if (target.is_empty() or target == drop.id) and _distance(player, drop) <= 3 and m3.add("boss_core" if drop.monsterId=="worldboss-1" else "scrap",1):
                    player.coins += rules.lootCoins
                    player.scrap += rules.lootScrap
                    player.message = "Beute: +4 Münzen, +1 Drohnenschrott."
                    drops.erase(drop)
                    return
            player.message = "Keine eigene Beute in Reichweite."

func step(dt: float, direction: Vector2) -> void:
    time += dt
    tick += 1
    direction=m3.farm_direction(dt,direction)
    if player.hp <= 0:
        if time >= player.respawn:
            player.hp = player.maxHp
            player.x = data.spawn.x
            player.z = data.spawn.z
            player.message = "An der Stadtwache wiederbelebt."
    else:
        var moving := direction.length() > .01
        direction = direction.limit_length()
        if moving:
            _move(player, direction.x * rules.speed * dt, direction.y * rules.speed * dt)
            player.yaw = atan2(-direction.x, -direction.y)
        var target_monster: Dictionary = {}
        for m in monsters:
            if m.id == player.target and m.hp > 0:
                target_monster = m
        if player.attacking and not target_monster.is_empty():
            var m: Dictionary = target_monster
            if not moving and _distance(player, m) <= rules.attackRange and time >= player.nextAttack:
                player.yaw = atan2(player.x - m.x, player.z - m.z)
                player.nextAttack = time + rules.attackSeconds/(1.4 if m3.effects.get("haste",0)>0 else 1.0)
                player.attackSerial += 1
                m3._hit(m,m3.attack())
        elif target_monster.is_empty():
            player.attacking = false
            player.target = ""
        var safe: bool = player.z > 2
        for m in monsters:
            if m.hp > 0 and _distance(player, m) < 8:
                safe = false
        if safe and tick % 20 == 0:
            player.hp = mini(int(player.maxHp), int(player.hp + 5))
    for i in range(monsters.size()):
        var m: Dictionary = monsters[i]
        if m.hp <= 0:
            if time >= m.respawn:
                m.hp = m.maxHp
                m.x = m.homeX
                m.z = m.homeZ
            continue
        if m.get("boss",false):
            if m.warning>0:
                m.warning=maxf(0,m.warning-dt)
                if m.warning==0:
                    if Vector2(player.x-m.warningX,player.z-m.warningZ).length()<5:
                        player.hp=maxi(0,player.hp-30);player.hurtSerial+=1;player.lastDamage=30
                        if player.hp==0:player.respawn=time+rules.playerRespawnSeconds;player.attacking=false
                    m.nextWave=time+6
            elif time>=m.nextWave and _distance(player,m)<rules.aggroRange:
                m.warning=2.5;m.warningX=player.x;m.warningZ=player.z
        var distance := _distance(player, m)
        var home_distance := Vector2(player.x - m.homeX, player.z - m.homeZ).length()
        if player.hp > 0 and (distance < rules.aggroRange or m.get("aggro",0)>0) and home_distance < 18:
            if distance > m.reach:
                _move(m, (player.x - m.x) / distance * m.speed * dt*(.5 if m.get("slow",0)>0 else 1.0), (player.z - m.z) / distance * m.speed * dt*(.5 if m.get("slow",0)>0 else 1.0))
            elif time >= m.nextAttack:
                m.nextAttack = time + rules.monsterAttackSeconds
                m.attackSerial += 1
                var incoming: int = maxi(1,int(rules.monsterDamage)-m3.defense()-(int(m3.attributes.resistance) if m.damageType=="arcane" else 0)-(int(m3.catalog.buffDefense) if m3.buff>0 else 0))
                var absorbed := false
                for summon in m3.summons:
                    if summon.kind=="golem" and summon.hp>0:summon.hp=maxi(0,summon.hp-incoming);absorbed=true;break
                if not absorbed and m3.pet.active and m3.pet.mode=="tank" and m3.pet.hp>0:
                    m3.pet.hp=maxi(0,m3.pet.hp-incoming)
                    if m3.pet.hp==0:m3.pet.recoveryUntil=Time.get_unix_time_from_system()+15
                    absorbed=true
                if not absorbed:
                    player.hp=maxi(0,player.hp-incoming)
                    if m.damageType=="arcane":m3.effects.weaken=4.0
                player.hurtSerial += 1
                player.lastDamage = rules.monsterDamage
                if player.hp == 0:
                    player.attacking = false
                    player.target = ""
                    player.respawn = time + rules.playerRespawnSeconds
                    player.message = "Besiegt. Wiederbelebung in 4 Sekunden. Kein EXP-Abzug in diesem Bezirk."
        else:
            var patrol_x: float = m.homeX + sin(time * .5 + i) * 1.3
            var dist := Vector2(patrol_x - m.x, m.homeZ - m.z).length()
            if dist > .1:
                _move(m, (patrol_x - m.x) / dist * dt, (m.homeZ - m.z) / dist * dt)
            if Vector2(m.x - m.homeX, m.z - m.homeZ).length() < 2:
                m.hp = m.maxHp
    for drop in drops.duplicate():
        if drop.expires < time:
            drops.erase(drop)

    m3.tick(dt)

func snapshot() -> Dictionary:
    var p: Dictionary = player.duplicate(true)
    p.dead = p.hp <= 0
    p.classId=m3.class_id
    p.name=m3.nickname
    var enemies: Array = monsters.filter(func(m):return _distance(player,m)<=m3.catalog.interestRadius).duplicate(true)
    for m in enemies:
        m.dead = m.hp <= 0
    return {"environment":{"phase":fmod(time,m3.catalog.daySeconds)/m3.catalog.daySeconds,"weather":["klar","Regen","Nebel"][int(time/m3.catalog.weatherSeconds)%3]},"summons":m3.summons.duplicate(true),"pets":m3.pets(), "type": "snapshot", "tick": tick, "players": [p], "monsters": enemies, "loot": drops.duplicate(true),
        "self": {"quest": p.quest, "kills": p.kills, "exp": p.exp, "level": p.level, "statPoints": p.statPoints,
        "coins": p.coins, "scrap": p.scrap, "message": p.message, "character":m3.view()}}
