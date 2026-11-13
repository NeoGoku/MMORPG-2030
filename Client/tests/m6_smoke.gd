extends SceneTree
const Practice=preload("res://scripts/practice.gd")
func _initialize() -> void:
    var sim=Practice.new()
    sim.player.level=10;sim.m3.attributes.strength=5;sim.m3.inventory.reset_stone=1
    sim.act("reset_stats","")
    assert(sim.player.statPoints==5 and sim.m3.attributes.strength==0 and not sim.m3.inventory.has("reset_stone"))
    sim.act("reset_stats","");assert(sim.player.statPoints==5)
    sim.player.level=100;sim.player.x=8.0;sim.player.z=6.0;sim.m3.inventory.boss_core=1
    sim.act("prestige","");assert(sim.m3.prestige==1)
    sim.free();print("PASS: local reset/prestige consume actual items once");quit()
