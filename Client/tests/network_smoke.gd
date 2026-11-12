extends SceneTree

const Network = preload("res://scripts/network.gd")
var first = Network.new()
var second = Network.new()
var elapsed := 0.0
var observed := false

func _initialize() -> void:
    root.add_child(first)
    root.add_child(second)
    second.snapshot_received.connect(_snapshot)
    first.connect_to_server("ws://127.0.0.1:5080/ws")
    second.connect_to_server("ws://127.0.0.1:5080/ws")

func _process(delta: float) -> bool:
    elapsed += delta
    first.direction = Vector2(1, 0)
    if observed:
        first.disconnect_from_server()
        second.disconnect_from_server()
        print("PASS: two Godot clients receive authoritative movement")
        quit()
    elif elapsed > 5.0:
        push_error("Godot clients did not observe shared movement")
        quit(1)
    return false

func _snapshot(players: Array) -> void:
    if first.player_id.is_empty() or second.player_id.is_empty() or first.player_id == second.player_id:
        return
    for player in players:
        if player.id == first.player_id and player.x > 0.5 and players.size() == 2:
            observed = true
