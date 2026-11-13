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
    scene.lobby.creator.open_for_class("Healer")
    scene.lobby.creator.selectors[1].select(2)
    scene.lobby.creator.update_preview()
    scene.lobby.creation_appearance=scene.lobby.creator.value()
    scene.lobby.creator.hide()
    await scene.lobby._create()
    if not check(scene.lobby.characters.size()==1,"Lobby creates selected owned slot"):return
    if not check(scene.lobby.characters[0].appearance==scene.lobby.creation_appearance,"Creation API persists selected preview appearance"):return
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
    scene._intent("appearance","female|2|1|3|2|1")
    for i in range(100):
        if scene.world.self.character.get("appearance","")=="female|2|1|3|2|1":break
        await physics_frame
    if not check(scene.world.self.character.get("appearance","")=="female|2|1|3|2|1","Appearance editor intent persists and synchronizes"):return
    if not check(scene.avatars[scene.local_id].appearance_key=="female|2|1|3|2|1","Public avatar consumes synchronized appearance"):return
    scene.graphics_options.apply_profile(0)
    if not check(scene.camera.far==75,"Mobile low profile changes actual render distance"):return
    scene.graphics_options.apply_profile(1)
    if not check(scene.minimap.state.players.size()>0,"Minimap consumes real world entities"):return
    scene._intent("track","mira")
    if not check(scene.quest_marker.visible and scene.minimap.destination!=Vector2.INF,"Quest tracking connects log to real world marker/minimap"):return
    scene._intent("track","")
    var original: Vector2=scene.hud.skill_buttons[0].position
    scene.hud.move_layout("skill1|left")
    if not check(scene.hud.skill_buttons[0].position.x==original.x-12,"User can reposition skill touch control"):return
    scene.hud.move_layout("reset")
    if not check(scene.hud.skill_buttons[0].position==original,"Touch layout reset restores playable positions"):return
    scene.menu.tab="Charakter"
    scene.menu.show()
    scene.menu._rebuild()
    if not check(scene.hud.skill_buttons.size()==6,"Six mobile skill buttons"):return
    scene.menu.tab="Skills"
    scene.menu._rebuild()
    if not check(scene.menu.info.skillbook.size()==8,"Eight real server skillbook entries reach touch menu"):return
    var picker: OptionButton
    var binding: Button
    for child in scene.menu.content.get_children():
        if child is OptionButton and picker==null:picker=child
        if child is Button and not child is OptionButton and child.text=="Auf gewählten Platz legen" and binding==null:binding=child
    if not check(picker!=null and binding!=null,"Skill binding controls are discoverable"):return
    picker.select(5)
    binding.pressed.emit()
    for i in range(100):
        if scene.world.self.character.skills[5].id=="skill1":break
        await physics_frame
    if not check(scene.world.self.character.skills[5].id=="skill1","Touch skill binding reaches authoritative six-slot bar"):return
    scene.menu.tab="Pet";scene.menu._rebuild()
    if not check(scene.menu.content.get_child_count()>15,"Pet management controls render from owned pet state"):return
    scene.menu.tab="Automatik";scene.menu._rebuild()
    if not check(not scene.menu.info.automation.active,"Opening automation menu does not start farming"):return
    scene.menu.hide()
    scene.social_panel.show();scene.social_panel.tab="Chat";scene.social_panel._show_tab()
    scene.social_panel.channel.select(0);scene.social_panel.message.text="M4 UI Nachricht"
    scene.social_panel.get_node("SendChat").pressed.emit()
    for i in range(150):
        if scene.social_panel.history.text.contains("M4 UI Nachricht"):break
        await physics_frame
    if not check(scene.social_panel.history.text.contains("M4 UI Nachricht"),"Touch chat sends and displays real server message"):return
    scene.social_panel.tab="Handel";scene.social_panel._show_tab()
    if not check(scene.social_panel.offer_item.item_count>0 and scene.social_panel.trade_box.visible,"Trade inventory and confirmation controls render"):return
    scene.social_panel.hide()
    scene.community_panel.show();scene.community_panel.tab="Wohnen";scene.community_panel.rebuild()
    if not check(scene.community_panel.homes.item_count==4,"Touch housing menu lists actual shared physical units"):return
    scene.community_panel.tab="Gilde";scene.community_panel.rebuild()
    if not check(scene.community_panel.names is LineEdit,"Guild creation is touch editable"):return
    scene.community_panel.tab="Auktion";scene.community_panel.rebuild()
    if not check(scene.community_panel.items.item_count>0 and scene.community_panel.price.max_value==100000,"Auction menu binds real inventory and bounded price"):return
    scene.community_panel.tab="Arena";scene.community_panel.rebuild()
    if not check(scene.community_panel.peers is OptionButton,"Arena selects real online peers"):return
    scene.community_city.update_world(scene.world,Vector3(40,0,34))
    if not check(scene.community_city.chunks.has("home-1"),"Physical housing chunk is actually created"):return
    scene.community_city.update_world(scene.world,Vector3(-80,0,-80))
    if not check(not scene.community_city.chunks.has("home-1"),"Distant housing chunk is unloaded"):return
    scene.community_panel.hide()
    scene.menu.tab="Skills";scene.menu.show();scene.menu._rebuild()
    await RenderingServer.frame_post_draw
    if not OS.get_environment("M3_SCREENSHOT").is_empty():root.get_texture().get_image().save_png(OS.get_environment("M3_SCREENSHOT"))
    scene.menu.hide()
    scene._select_local(2,"Wanderin","Archer")
    if not check(not scene.online and scene.practice.m3.class_id=="Archer","Separate local slot/class switch preserves offline play"):return
    scene.queue_free()
    await process_frame
    print("PASS: account lobby, owned character slot/class, auth ticket, rendered pet, M4/M5 skill/chat/housing/guild/auction/arena UI and housing streaming and local slot separation")
    quit()
