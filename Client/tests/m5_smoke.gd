extends SceneTree
const Practice=preload("res://scripts/practice.gd")
func _initialize() -> void:
    var sim=Practice.new()
    sim.player.x=0.0;sim.player.z=-70.0
    var snapshot: Dictionary=sim.snapshot()
    assert(snapshot.monsters.any(func(m):return m.id=="worldboss-1" and m.maxHp==900),"local open dungeon and boss exist")
    sim.step(.05,Vector2.ZERO)
    assert(sim.snapshot().monsters.any(func(m):return m.id=="worldboss-1" and m.get("warning",0)>0),"boss warns before damaging")
    assert(not sim._walkable(31,34),"shared housing wall blocks local simulation")
    sim.free();print("PASS: local connected dungeon/boss telegraph and housing collision");quit()
