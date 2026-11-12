extends Node3D

const Network = preload("res://scripts/network.gd")
var network = Network.new()
var avatars: Dictionary = {}
var targets: Dictionary = {}
var status: Label
var endpoint: LineEdit
var touch := Vector2.ZERO
var touch_buttons: Array[Button] = []

func _ready() -> void:
    add_child(network)
    network.snapshot_received.connect(_update_players)
    network.state_changed.connect(func(message: String): status.text = message)
    _build_ui()

func _build_ui() -> void:
    var layer := CanvasLayer.new()
    add_child(layer)
    var panel := VBoxContainer.new()
    panel.position = Vector2(20, 20)
    layer.add_child(panel)
    var title := Label.new()
    title.text = "MMORPG-2030 · Multiplayer foundation"
    panel.add_child(title)
    endpoint = LineEdit.new()
    endpoint.placeholder_text = "wss://your-server/ws"
    endpoint.text = "ws://127.0.0.1:5080/ws" if not OS.has_feature("web") else ""
    endpoint.custom_minimum_size.x = 500
    panel.add_child(endpoint)
    var connect_button := Button.new()
    connect_button.text = "Connect / reconnect"
    connect_button.pressed.connect(func(): network.connect_to_server(endpoint.text.strip_edges()))
    panel.add_child(connect_button)
    var disconnect_button := Button.new()
    disconnect_button.text = "Disconnect"
    disconnect_button.pressed.connect(network.disconnect_from_server)
    panel.add_child(disconnect_button)
    status = Label.new()
    status.text = "Enter server address to connect. Sessions are temporary."
    panel.add_child(status)
    var controls := HBoxContainer.new()
    controls.position = Vector2(20, 620)
    layer.add_child(controls)
    var labels := ["←", "↑", "↓", "→"]
    for label in labels:
        var button := Button.new()
        button.text = label
        button.custom_minimum_size = Vector2(80, 80)
        button.focus_mode = Control.FOCUS_NONE
        controls.add_child(button)
        touch_buttons.append(button)

func _process(delta: float) -> void:
    var keyboard := Vector2.ZERO
    if not endpoint.has_focus():
        keyboard = Vector2(float(Input.is_physical_key_pressed(KEY_D) or Input.is_physical_key_pressed(KEY_RIGHT)) - float(Input.is_physical_key_pressed(KEY_A) or Input.is_physical_key_pressed(KEY_LEFT)), float(Input.is_physical_key_pressed(KEY_S) or Input.is_physical_key_pressed(KEY_DOWN)) - float(Input.is_physical_key_pressed(KEY_W) or Input.is_physical_key_pressed(KEY_UP)))
    touch = Vector2(float(touch_buttons[3].is_pressed()) - float(touch_buttons[0].is_pressed()), float(touch_buttons[2].is_pressed()) - float(touch_buttons[1].is_pressed()))
    network.direction = (keyboard + touch).limit_length() if DisplayServer.window_is_focused() else Vector2.ZERO
    for id in avatars:
        avatars[id].position = avatars[id].position.lerp(targets[id], minf(1.0, delta * 15.0))
    if avatars.has(network.player_id):
        var player: Node3D = avatars[network.player_id]
        $Camera3D.position = player.position + Vector3(0, 6, 12)

func _update_players(players: Array) -> void:
    var seen: Dictionary = {}
    for player in players:
        if not player is Dictionary or not player.get("id") is String:
            continue
        var id: String = player.id
        var position_target := Vector3(float(player.get("x", 0)), 1.0, float(player.get("z", 0)))
        seen[id] = true
        if not avatars.has(id):
            var avatar := MeshInstance3D.new()
            avatar.mesh = CapsuleMesh.new()
            var material := StandardMaterial3D.new()
            material.albedo_color = Color(0.2, 0.8, 0.5) if id == network.player_id else Color(0.3, 0.5, 1.0)
            avatar.material_override = material
            avatar.position = position_target
            add_child(avatar)
            avatars[id] = avatar
        targets[id] = position_target
    for id in avatars.keys():
        if not seen.has(id):
            avatars[id].queue_free()
            avatars.erase(id)
            targets.erase(id)
