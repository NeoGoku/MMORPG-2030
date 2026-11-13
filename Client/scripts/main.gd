extends Node3D

const Network = preload("res://scripts/network.gd")
const Practice = preload("res://scripts/practice.gd")
const Actor = preload("res://scripts/actor.gd")
const SocialPanel = preload("res://scripts/social_panel.gd")
var social_panel = SocialPanel.new()
const CommunityPanel=preload("res://scripts/community_panel.gd")
const CommunityCity=preload("res://scripts/community_city.gd")
var community_panel=CommunityPanel.new()
var community_city=CommunityCity.new()
var summon_nodes: Dictionary = {}
const Lobby = preload("res://scripts/lobby.gd")
const CharacterMenu = preload("res://scripts/character_menu.gd")
var menu = CharacterMenu.new()
var lobby = Lobby.new()
var pet_nodes: Dictionary = {}
var local_slots: Dictionary = {}
var local_slot := 0
const HUD = preload("res://scripts/hud.gd")

var network = Network.new()
var practice = Practice.new()
var hud = HUD.new()
var avatars: Dictionary = {}
var monsters: Dictionary = {}
var loot_nodes: Dictionary = {}
var world: Dictionary = {}
var local_id := "local"
var online := false
var local_player: Dictionary = {}
var camera_rig := Node3D.new()
var spring := SpringArm3D.new()
var camera := Camera3D.new()
var yaw := 0.0
var pitch := -.38
var camera_touch := -1
var practice_time := 0.0
var npc

func _ready() -> void:
    add_child(network)
    add_child(practice)
    var layer := CanvasLayer.new()
    add_child(layer)
    layer.add_child(hud)
    hud.action_requested.connect(_action)
    hud.connect_requested.connect(_connect)
    hud.practice_requested.connect(_start_practice)
    hud.add_child(menu)
    hud.character_menu = menu
    menu.intent.connect(_intent)
    hud.add_child(social_panel)
    social_panel.intent.connect(_social_intent)
    add_child(community_city)
    hud.add_child(community_panel)
    community_panel.intent.connect(_social_intent)
    var world_button=hud._button(hud,"Wohnen / Gilde / Börse",Rect2(22,258,240,44),"online")
    world_button.pressed.connect(func():community_panel.visible=not community_panel.visible;community_panel.rebuild())
    var social_button=hud._button(hud,"Gruppe / Chat / Handel",Rect2(22,206,240,44),"online")
    social_button.pressed.connect(func():social_panel.visible=not social_panel.visible;social_panel._refresh_view())
    hud.online_panel.queue_free()
    hud.online_panel = lobby
    hud.add_child(lobby)
    lobby.play_requested.connect(_connect)
    lobby.local_requested.connect(_select_local)
    network.world_received.connect(_receive_world)
    network.state_changed.connect(_network_status)
    camera_rig.name = "CameraRig"
    add_child(camera_rig)
    spring.spring_length = 8.5
    spring.margin = .3
    spring.collision_mask = 1
    camera_rig.add_child(spring)
    camera.fov = 65
    camera.near = .15
    spring.add_child(camera)
    camera.current = true
    npc = Actor.new()
    npc.kind = "npc"
    npc.tint = Color("c39a52")
    npc.display_name = practice.data.npc.name
    npc.position = Vector3(practice.data.npc.x,.18,practice.data.npc.z)
    npc.target_position = npc.position
    add_child(npc)
    var elin=Actor.new()
    elin.kind="npc";elin.tint=Color("719a86");elin.display_name="Elin · Wohnamt / Gildenregister"
    elin.position=Vector3(30,.18,24);elin.target_position=elin.position;add_child(elin)
    _services()
    _receive_world(practice.snapshot())
    camera_rig.position = Vector3(local_player.x,2.0,local_player.z)

func _connect(url: String, ticket: String = "") -> void:
    online = true
    local_id = ""
    _clear_dynamic()
    local_player = {}
    hud.joystick.reset()
    hud.mode_label.text = "ONLINE · VERBINDUNG WIRD AUFGEBAUT"
    network.auth_ticket = ticket
    network.connect_to_server(url)

func _start_practice() -> void:
    network.disconnect_from_server()
    online = false
    local_id = "local"
    _clear_dynamic()
    practice.free()
    practice = Practice.new()
    add_child(practice)
    hud.joystick.reset()
    _receive_world(practice.snapshot())

func _network_status(message: String) -> void:
    if online:
        hud.message.text = message
        if network.player_id.is_empty():
            hud.mode_label.text = "ONLINE · NICHT VERBUNDEN"
            if not local_id.is_empty():
                _clear_dynamic()
                local_player = {}
                local_id = ""
        else:
            local_id = network.player_id

func _clear_dynamic() -> void:
    for collection in [avatars, monsters, loot_nodes, pet_nodes,summon_nodes]:
        for node in collection.values():
            node.queue_free()
        collection.clear()
    world = {}

func _physics_process(delta: float) -> void:
    var input := Vector2.ZERO
    if not hud.online_panel.visible and not menu.visible and not social_panel.visible and not community_panel.visible:
        input = hud.joystick.direction
        input += Vector2(float(Input.is_physical_key_pressed(KEY_D) or Input.is_physical_key_pressed(KEY_RIGHT))-float(Input.is_physical_key_pressed(KEY_A) or Input.is_physical_key_pressed(KEY_LEFT)), float(Input.is_physical_key_pressed(KEY_S) or Input.is_physical_key_pressed(KEY_DOWN))-float(Input.is_physical_key_pressed(KEY_W) or Input.is_physical_key_pressed(KEY_UP)))
    if not DisplayServer.window_is_focused():
        input = Vector2.ZERO
    var direction := Vector2(input.x*cos(yaw)+input.y*sin(yaw), -input.x*sin(yaw)+input.y*cos(yaw)).limit_length()
    if online:
        network.direction = direction
    else:
        practice_time += minf(delta,.2)
        while practice_time >= .05:
            practice_time -= .05
            practice.step(.05, direction)
        _receive_world(practice.snapshot())
    if not local_player.is_empty():
        var follow := Vector3(local_player.x,2.0,local_player.z)
        if avatars.has(local_id):
            follow = avatars[local_id].position + Vector3(0,2.0,0)
        camera_rig.position = camera_rig.position.lerp(follow,minf(1,delta*12))
    camera_rig.rotation.y = yaw
    spring.rotation.x = pitch

func _receive_world(state: Dictionary) -> void:
    if online:
        if network.player_id.is_empty():
            return
        local_id = network.player_id
    world = state
    local_player = {}
    for p in state.players:
        if p.id == local_id:
            local_player = p
    var selected: String = local_player.get("target", "")
    _sync_actors(state.players, avatars, "player", selected)
    _sync_actors(state.monsters, monsters, "monster", selected)
    _sync_loot(state.loot)
    _sync_pets(state.get("pets",[]))
    _sync_summons(state.get("summons",[]))
    social_panel.update_state(state,online)
    community_panel.update_state(state,online)
    if not local_player.is_empty():community_city.update_world(state,Vector3(local_player.x,0,local_player.z))
    if not local_player.is_empty():$City.apply_environment(state.get("environment",{}),Vector3(local_player.x,0,local_player.z))
    hud.update_state(state,local_player,online)
    if not local_player.is_empty():
        var nearby := Vector2(local_player.x-practice.data.npc.x,local_player.z-practice.data.npc.z).length() <= 3
        hud.interact_button.modulate = Color("ffdf9c") if nearby else Color.WHITE
        hud.loot_button.modulate = Color("ffdf9c") if not state.loot.is_empty() else Color.WHITE

func _sync_actors(states: Array, nodes: Dictionary, kind: String, selected: String) -> void:
    var seen: Dictionary = {}
    for state in states:
        seen[state.id] = true
        if not nodes.has(state.id):
            var actor = Actor.new()
            actor.kind = kind
            actor.monster_variant="boss" if state.get("boss",false) else ("golem" if state.get("level",1)>1 else "drone")
            actor.class_id=state.get("classId",practice.m3.class_id if state.id==local_id else "Shield Sword")
            actor.tint = Color("58cdb3") if state.id == local_id else Color("719ed4")
            actor.display_name = ("Du · "+state.get("name","Reisender")) if state.id == local_id else (state.get("name","Reisender "+state.id.substr(0,4)) if kind == "player" else state.get("name","Gegner")+" · St. "+str(state.get("level",1)))
            actor.position = Vector3(state.x,.18,state.z)
            add_child(actor)
            nodes[state.id] = actor
        nodes[state.id].apply_state(state,state.id==selected)
    for id in nodes.keys():
        if not seen.has(id):
            nodes[id].queue_free()
            nodes.erase(id)

func _sync_loot(drops: Array) -> void:
    var seen: Dictionary = {}
    for drop in drops:
        seen[drop.id] = true
        if not loot_nodes.has(drop.id):
            var mesh := MeshInstance3D.new()
            var box := BoxMesh.new()
            box.size = Vector3(.55,.55,.55)
            mesh.mesh = box
            var material := StandardMaterial3D.new()
            material.albedo_color = Color("edc56f")
            material.emission_enabled = true
            material.emission = Color("a57534")
            mesh.material_override = material
            mesh.position = Vector3(drop.x,.5,drop.z)
            add_child(mesh)
            loot_nodes[drop.id] = mesh
    for id in loot_nodes.keys():
        if not seen.has(id):
            loot_nodes[id].queue_free()
            loot_nodes.erase(id)

func _action(kind: String) -> void:
    if local_player.is_empty() or hud.online_panel.visible:
        return
    if kind.begins_with("slot:"):
        _cast_slot(int(kind.get_slice(":",1)))
        return
    var target_id := ""
    if kind == "interact":
        var closest := Vector2(local_player.x-practice.data.npc.x,local_player.z-practice.data.npc.z).length()
        var service := ""
        for key in practice.m3.catalog.services:
            var pos: Dictionary = practice.m3.catalog.services[key]
            var dist := Vector2(local_player.x-pos.x,local_player.z-pos.z).length()
            if dist<=3 and dist<closest:closest=dist;service=key
        for node in practice.m3.catalog.nodes:
            var dist := Vector2(local_player.x-node.x,local_player.z-node.z).length()
            if dist<=3 and dist<closest:closest=dist;service="node:"+node.id
        if service in ["merchant","workbench"]:
            menu.tab="Inventar";menu.show();menu._rebuild();return
        if service in ["teleporter","return"]:_intent("teleport","");return
        if service.begins_with("node:"):_intent("gather",service.get_slice(":",1));return
    if kind == "target":
        var candidates: Array = []
        for enemy in world.monsters:
            var distance := Vector2(enemy.x-local_player.x,enemy.z-local_player.z).length()
            if not enemy.dead and distance <= 24:
                candidates.append({"id": enemy.id, "distance": distance})
        candidates.sort_custom(func(a: Dictionary,b: Dictionary):return a.distance<b.distance)
        if candidates.is_empty():
            return
        var index := -1
        for i in range(candidates.size()):
            if candidates[i].id == local_player.target:
                index = i
        target_id = candidates[(index+1)%candidates.size()].id
    if online:
        network.action(kind,target_id)
    else:
        practice.act(kind,target_id)
        _receive_world(practice.snapshot())

func _unhandled_input(event: InputEvent) -> void:
    if event is InputEventKey and event.pressed and not event.echo and not hud.online_panel.visible and not menu.visible and not social_panel.visible and not community_panel.visible:
        match event.physical_keycode:
            KEY_TAB: _action("target"); get_viewport().set_input_as_handled()
            KEY_SPACE: _action("attack")
            KEY_E: _action("interact")
            KEY_F: _action("loot")
            KEY_1, KEY_2, KEY_3, KEY_4, KEY_5, KEY_6: _cast_slot(event.physical_keycode-KEY_1)
    if event is InputEventScreenTouch:
        if event.pressed and event.position.x > get_viewport().get_visible_rect().size.x*.4 and camera_touch == -1:
            camera_touch = event.index
        elif not event.pressed and event.index == camera_touch:
            camera_touch = -1
    elif event is InputEventScreenDrag and event.index == camera_touch:
        yaw -= event.relative.x*.006
        pitch = clampf(pitch-event.relative.y*.004,-.9,-.12)
    elif event is InputEventMouseMotion and Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
        yaw -= event.relative.x*.005
        pitch = clampf(pitch-event.relative.y*.004,-.9,-.12)

func _notification(what: int) -> void:
    if what == NOTIFICATION_APPLICATION_FOCUS_OUT:
        camera_touch = -1
        network.direction = Vector2.ZERO

func _intent(kind: String,target_id: String) -> void:
    if kind=="layout":hud.move_layout(target_id);return
    if local_player.is_empty():return
    if online: network.action(kind,target_id)
    else:
        practice.act(kind,target_id)
        _receive_world(practice.snapshot())

func _select_local(slot: int,nickname: String,class_id: String) -> void:
    if not online:
        local_slots[local_slot] = {"player":practice.player.duplicate(true),"classId":practice.m3.class_id,"name":practice.m3.nickname,"stats":practice.m3.attributes.duplicate(),"inventory":practice.m3.inventory.duplicate(),"equipment":practice.m3.equipment.duplicate(),"professions":practice.m3.professions.duplicate(),"quests":practice.m3.quest_progress.duplicate(),"claimed":practice.m3.claimed.duplicate(),"pet":practice.m3.pet.duplicate(true),"farmXp":practice.m3.farm_xp,"farmed":practice.m3.farmed,"ownedPets":practice.m3.owned_pets.duplicate(true),"skillbar":practice.m3.skill_bar.duplicate()}
    _start_practice()
    local_slot = slot
    if local_slots.has(slot):
        var saved: Dictionary = local_slots[slot]
        practice.player = saved.player.duplicate(true)
        practice.m3.class_id = saved.classId
        practice.m3.nickname = saved.name
        practice.m3.attributes = saved.stats.duplicate()
        practice.m3.inventory = saved.inventory.duplicate()
        practice.m3.equipment = saved.equipment.duplicate()
        practice.m3.professions = saved.professions.duplicate()
        practice.m3.quest_progress = saved.quests.duplicate()
        practice.m3.claimed = saved.claimed.duplicate()
        practice.m3.pet = saved.pet.duplicate(true)
        practice.m3.farm_xp = saved.farmXp
        practice.m3.farmed = saved.farmed
        practice.m3.owned_pets=saved.get("ownedPets",[]).duplicate(true)
        practice.m3.skill_bar=saved.get("skillbar",practice.m3.skill_bar).duplicate()
    else:
        practice.m3.class_id = class_id
        practice.m3.nickname = nickname
    _receive_world(practice.snapshot())

func _sync_pets(states: Array) -> void:
    var seen: Dictionary = {}
    for pet in states:
        seen[pet.owner] = true
        if not pet_nodes.has(pet.owner):
            var node = Actor.new()
            node.kind = "pet"
            node.display_name = "Arkaner Funke"
            node.position = Vector3(pet.x,.2,pet.z)
            add_child(node)
            pet_nodes[pet.owner] = node
        var actor = pet_nodes[pet.owner]
        actor.target_position = Vector3(pet.x,.2,pet.z)
        actor.scale=Vector3.ONE*(1+pet.get("evolution",0)*.15)
        actor.hp_label.text = "St. %d · %s · %d HP"%[pet.level,pet.mode,pet.get("hp",100)]
    for key in pet_nodes.keys():
        if not seen.has(key):pet_nodes[key].queue_free();pet_nodes.erase(key)

func _services() -> void:
    var names := {"merchant":"Händler · Aufträge", "workbench":"Werkbank", "teleporter":"Portal · Südpark", "return":"Portal · Nordtor"}
    for key in practice.m3.catalog.services:
        var service: Dictionary = practice.m3.catalog.services[key]
        var actor = Actor.new()
        actor.kind = "npc"
        actor.tint = Color("90a6d2")
        actor.display_name = names[key]
        actor.position = Vector3(service.x,.18,service.z)
        actor.target_position = actor.position
        add_child(actor)
    for node in practice.m3.catalog.nodes:
        var marker := MeshInstance3D.new()
        var shape := SphereMesh.new()
        shape.radius = .6
        shape.height = 1.2
        marker.mesh = shape
        var material := StandardMaterial3D.new()
        material.albedo_color = Color("75b984") if node.id=="herb" else Color("93836b")
        marker.material_override = material
        marker.position = Vector3(node.x,.7,node.z)
        add_child(marker)
        var label := Label3D.new()
        label.text = node.name+" [E]"
        label.font_size = 24
        label.pixel_size = .01
        label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
        label.position = Vector3(node.x,1.8,node.z)
        add_child(label)

func _cast_slot(index: int) -> void:
    var character: Dictionary=world.get("self",{}).get("character",{})
    if index>=0 and index<character.get("skills",[]).size():_intent("skill",character.skills[index].id)

func _social_intent(kind: String,target: String,text: String) -> void:
    if online:network.action(kind,target,text)
    else:hud.message.text="Diese Aktion benötigt echte Mitspieler auf einem Online-Server."

func _sync_summons(states: Array) -> void:
    if states==null:states=[]
    var seen: Dictionary={}
    for state in states:
        seen[state.owner]=true
        if not summon_nodes.has(state.owner):
            var actor=Actor.new()
            actor.kind="monster" if state.kind=="wolf" else ("pet" if state.kind=="wisp" else "npc")
            actor.tint=Color("b69de1")
            actor.display_name="Beschwörung · "+state.kind
            actor.position=Vector3(state.x,.2,state.z)
            add_child(actor)
            summon_nodes[state.owner]=actor
        summon_nodes[state.owner].target_position=Vector3(state.x,.2,state.z)
        summon_nodes[state.owner].hp_label.text="%d HP · %.0fs"%[state.hp,state.remaining]
    for key in summon_nodes.keys():
        if not seen.has(key):summon_nodes[key].queue_free();summon_nodes.erase(key)
