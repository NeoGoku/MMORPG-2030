extends SceneTree

const Practice = preload("res://scripts/practice.gd")
var sim = Practice.new()

func _initialize() -> void:
    var s: Dictionary = sim.snapshot()
    assert(s.players.size() == 1 and s.monsters.size() == 3, "offline practice has one real local character and three drones")
    sim.act("interact", "")
    assert(sim.snapshot().self.quest == "available", "NPC range checked")
    sim.player.x = -3.0
    sim.player.z = 7.0
    sim.act("interact", "")
    assert(sim.snapshot().self.quest == "active", "quest accepted")
    for enemy in sim.monsters.filter(func(m):return str(m.id).begins_with("drone-")):
        sim.player.x = enemy.x
        sim.player.z = enemy.z + 2.0
        sim.act("target", enemy.id)
        for i in range(3):
            sim.step(.05, Vector2.ZERO)
        assert(sim.player.target == enemy.id, "idle selection persists")
        sim.act("attack", "")
        for i in range(70):
            sim.step(0.05, Vector2.ZERO)
        assert(enemy.hp == 0, "stationary attacks kill drone")
        var drop: Dictionary = sim.drops.back()
        sim.player.x = drop.x
        sim.player.z = drop.z
        sim.act("loot", drop.id)
        var coins: int = sim.player.coins
        sim.act("loot", drop.id)
        assert(sim.player.coins == coins, "loot cannot be collected twice")
    assert(sim.snapshot().self.quest == "ready", "quest objective reached")
    sim.player.x = -3.0
    sim.player.z = 6.0
    sim.act("interact", "")
    s = sim.snapshot()
    assert(s.self.quest == "complete" and s.self.level == 2 and s.self.exp == 20 and s.self.coins == 37 and s.self.scrap == 3 and s.self.statPoints == 1, "quest, XP, level and loot rewards")
    sim.act("interact", "")
    assert(sim.player.coins == 37, "turn-in reward once")
    sim.player.x = -11.0
    sim.player.z = 10.0
    for i in range(50):
        sim.step(.05, Vector2.LEFT)
    assert(sim.player.x > -11.6, "buildings block movement")
    sim.free()
    print("PASS: local practice quest/combat/loot/level/collision scenarios")
    quit()
