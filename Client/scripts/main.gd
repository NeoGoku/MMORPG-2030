extends Node3D

const Network = preload("res://scripts/network.gd")
const Practice = preload("res://scripts/practice.gd")
const Actor = preload("res://scripts/actor.gd")
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
    _receive_world(practice.snapshot())
    camera_rig.position = Vector3(local_player.x,2.0,local_player.z)

func _connect(url: String) -> void:
    online = true
    local_id = ""
    _clear_dynamic()
    local_player = {}
    hud.joystick.reset()
    hud.mode_label.text = "ONLINE · VERBINDUNG WIRD AUFGEBAUT"
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
    for collection in [avatars, monsters, loot_nodes]:
        for node in collection.values():
            node.queue_free()
        collection.clear()
    world = {}

func _physics_process(delta: float) -> void:
    var input := Vector2.ZERO
    if not hud.online_panel.visible:
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
            actor.tint = Color("58cdb3") if state.id == local_id else Color("719ed4")
            actor.display_name = "Du" if state.id == local_id else ("Reisender " + state.id.substr(0,4) if kind == "player" else "Wachdrohne · St. 1")
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
    var target_id := ""
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
    if event is InputEventKey and event.pressed and not event.echo and not hud.online_panel.visible:
        match event.physical_keycode:
            KEY_TAB: _action("target"); get_viewport().set_input_as_handled()
            KEY_SPACE: _action("attack")
            KEY_E: _action("interact")
            KEY_F: _action("loot")
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
