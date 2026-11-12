extends Node

# Explicit local training; no simulated remote players. Uses the server's district/rules.
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
    for spawn in data.monsters:
        monsters.append({"id": spawn.id, "name": spawn.name, "x": spawn.x, "z": spawn.z,
            "homeX": spawn.x, "homeZ": spawn.z, "hp": rules.monsterHp, "maxHp": rules.monsterHp,
            "attackSerial": 0, "hurtSerial": 0, "lastDamage": 0, "nextAttack": 0.0, "respawn": 0.0})

func _distance(a: Dictionary, b: Dictionary) -> float:
    return Vector2(a.x - b.x, a.z - b.z).length()

func _walkable(x: float, z: float) -> bool:
    if absf(x) > data.bounds - .45 or absf(z) > data.bounds - .45:
        return false
    for b in data.buildings:
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
    while player.level < 100 and player.exp >= player.level * 100:
        player.exp -= player.level * 100
        player.level += 1
        player.statPoints += 1

func act(kind: String, target: String = "") -> void:
    if player.hp <= 0:
        return
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
                player.hp = rules.playerHp
                player.message = "Mira: Nordtor ist sicherer. Danke! +60 EXP, +25 Münzen."
            else:
                player.message = "Mira: Gute Arbeit!" if player.quest == "complete" else "Mira: Noch %d Drohnen im Nordhof." % (rules.questKills - player.kills)
        "loot":
            for drop in drops:
                if (target.is_empty() or target == drop.id) and _distance(player, drop) <= 3:
                    player.coins += rules.lootCoins
                    player.scrap += rules.lootScrap
                    player.message = "Beute: +4 Münzen, +1 Drohnenschrott."
                    drops.erase(drop)
                    return
            player.message = "Keine eigene Beute in Reichweite."

func step(dt: float, direction: Vector2) -> void:
    time += dt
    tick += 1
    if player.hp <= 0:
        if time >= player.respawn:
            player.hp = rules.playerHp
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
                player.nextAttack = time + rules.attackSeconds
                player.attackSerial += 1
                m.hp = maxi(0, int(m.hp - rules.playerDamage))
                m.hurtSerial += 1
                m.lastDamage = rules.playerDamage
                if m.hp == 0:
                    m.respawn = time + rules.monsterRespawnSeconds
                    player.attacking = false
                    _exp(int(rules.killExp))
                    if player.quest == "active":
                        player.kills += 1
                        if player.kills >= rules.questKills:
                            player.quest = "ready"
                    drop_sequence += 1
                    drops.append({"id": "drop-%d" % drop_sequence, "monsterId": m.id, "x": m.x, "z": m.z, "expires": time + 90})
                    player.message = "Drohne besiegt. +20 EXP. Beute in der Nähe einsammeln."
        elif target_monster.is_empty():
            player.attacking = false
            player.target = ""
        var safe: bool = player.z > 2
        for m in monsters:
            if m.hp > 0 and _distance(player, m) < 8:
                safe = false
        if safe and tick % 20 == 0:
            player.hp = mini(int(rules.playerHp), int(player.hp + 5))
    for i in range(monsters.size()):
        var m: Dictionary = monsters[i]
        if m.hp <= 0:
            if time >= m.respawn:
                m.hp = rules.monsterHp
                m.x = m.homeX
                m.z = m.homeZ
            continue
        var distance := _distance(player, m)
        var home_distance := Vector2(player.x - m.homeX, player.z - m.homeZ).length()
        if player.hp > 0 and distance < rules.aggroRange and home_distance < 12:
            if distance > 2:
                _move(m, (player.x - m.x) / distance * rules.monsterSpeed * dt, (player.z - m.z) / distance * rules.monsterSpeed * dt)
            elif time >= m.nextAttack:
                m.nextAttack = time + rules.monsterAttackSeconds
                m.attackSerial += 1
                player.hp = maxi(0, int(player.hp - rules.monsterDamage))
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
                m.hp = rules.monsterHp
    for drop in drops.duplicate():
        if drop.expires < time:
            drops.erase(drop)

func snapshot() -> Dictionary:
    var p: Dictionary = player.duplicate(true)
    p.dead = p.hp <= 0
    var enemies: Array = monsters.duplicate(true)
    for m in enemies:
        m.dead = m.hp <= 0
    return {"type": "snapshot", "tick": tick, "players": [p], "monsters": enemies, "loot": drops.duplicate(true),
        "self": {"quest": p.quest, "kills": p.kills, "exp": p.exp, "level": p.level, "statPoints": p.statPoints,
        "coins": p.coins, "scrap": p.scrap, "message": p.message}}
