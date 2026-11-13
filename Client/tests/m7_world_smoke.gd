extends SceneTree

func _initialize() -> void:
    call_deferred("run")

func check(value: bool,message: String) -> void:
    if not value:push_error(message);quit(1)
    else:print("PASS M7 world: "+message)

func run() -> void:
    var regions=load("res://scripts/world_regions.gd").new()
    root.add_child(regions)
    await process_frame
    regions.update_focus(Vector3.ZERO)
    check(regions.loaded.is_empty(),"distant area geometry remains unloaded")
    regions.update_focus(Vector3(96,0,-74))
    check(regions.loaded.has("helix"),"Helix laboratory and field NPCs stream in")
    regions.update_focus(Vector3(-92,0,96))
    check(not regions.loaded.has("helix") and regions.loaded.has("ashgrove"),"hysteresis unloads distant area and loads shrine")
    var sim=load("res://scripts/practice.gd").new()
    sim.player.level=20;sim.player.x=48;sim.player.z=-34
    sim.act("world_transit","helix")
    check(sim.player.x==78 and sim.player.z==-52,"practice uses the same configured transit endpoints")
    check(sim.loot_item("worldboss-helix")=="storm_core","practice retains configured boss loot")
    regions.queue_free();sim.free()
    await process_frame
    quit()
