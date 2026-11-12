extends SceneTree
const Practice = preload("res://scripts/practice.gd")
func _initialize() -> void:
    for cls in JSON.parse_string(FileAccess.get_file_as_string("res://data/game.json")).classes:
        var sim = Practice.new()
        sim.m3.class_id = cls
        sim.player.x = -6.0
        sim.player.z = -7.0
        sim.act("target","drone-1")
        sim.act("skill","skill1")
        var hp: int = sim.monsters[0].hp
        assert(hp<42,"class skill damage")
        sim.act("skill","skill1")
        assert(sim.monsters[0].hp==hp,"cooldown stops repeated cast")
        sim.act("skill","skill2")
        assert(sim.m3.mana==75,"two working skills per class")
        sim.player.statPoints = 1
        sim.act("stat","vitality")
        assert(sim.player.statPoints==0 and sim.player.maxHp==108,"attribute allocation")
        sim.m3.inventory.ore = 3
        sim.player.x = -8.0
        sim.player.z = 6.0
        sim.act("craft","iron_sword")
        sim.act("equip","iron_sword")
        assert(sim.m3.equipment.weapon=="iron_sword" and not sim.m3.inventory.has("ore"),"craft and equip")
        sim.act("pet","heal")
        assert(sim.snapshot().pets.size()==1,"one active pet")
        sim.free()
    print("PASS: eight local classes, skills/cooldown/mana, stats, crafting/equipment and active pet")
    quit()
