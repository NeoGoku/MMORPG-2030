extends SceneTree
var scene
func _initialize() -> void:
    run.call_deferred()
func check(ok: bool,message: String) -> bool:
    if not ok:push_error(message);quit(1)
    return ok
func run() -> void:
    scene=load("res://scenes/main.tscn").instantiate()
    root.add_child(scene)
    for i in range(5):await process_frame
    scene.lobby.show()
    scene.lobby.base_url.text="http://127.0.0.1:5081"
    scene.lobby.username.text="ui_"+str(Time.get_ticks_usec())
    scene.lobby.password.text="UI-Test-Only-Password-123!"
    await scene.lobby._login(true)
    if not check(not scene.lobby.token.is_empty(),"Lobby can register and load owned slots"):return
    scene.lobby.nickname.text="Heilerin"
    scene.lobby.slots.select(1)
    var keys: Array=scene.lobby.catalog.classes.keys()
    scene.lobby.classes.select(keys.find("Healer"))
    await scene.lobby._create()
    if not check(scene.lobby.characters.size()==1,"Lobby creates selected owned slot"):return
    await scene.lobby._play()
    for i in range(300):
        if scene.world.get("self",{}).get("character",{}).get("classId","")=="Healer":break
        await physics_frame
    if not check(scene.online and scene.world.get("self",{}).get("character",{}).get("classId","")=="Healer","One-use ticket connects selected class"):return
    scene._intent("pet","heal")
    for i in range(100):
        if scene.pet_nodes.size()==1:break
        await physics_frame
    if not check(scene.pet_nodes.size()==1,"Server pet appears in rendered client"):return
    var original: Vector2=scene.hud.skill_buttons[0].position
    scene.hud.move_layout("skill1|left")
    if not check(scene.hud.skill_buttons[0].position.x==original.x-12,"User can reposition skill touch control"):return
    scene.hud.move_layout("reset")
    if not check(scene.hud.skill_buttons[0].position==original,"Touch layout reset restores playable positions"):return
    scene.menu.tab="Charakter"
    scene.menu.show()
    scene.menu._rebuild()
    if not check(scene.hud.skill_buttons.size()==6,"Six mobile skill buttons"):return
    await RenderingServer.frame_post_draw
    if not OS.get_environment("M3_SCREENSHOT").is_empty():root.get_texture().get_image().save_png(OS.get_environment("M3_SCREENSHOT"))
    scene.menu.hide()
    scene._select_local(2,"Wanderin","Archer")
    if not check(not scene.online and scene.practice.m3.class_id=="Archer","Separate local slot/class switch preserves offline play"):return
    scene.queue_free()
    await process_frame
    print("PASS: account lobby, owned character slot/class, auth ticket, rendered pet and local slot separation")
    quit()
