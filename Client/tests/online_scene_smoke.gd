extends SceneTree

const Network = preload("res://scripts/network.gd")
var scene
var peer = Network.new()
var elapsed := 0.0
var remote_origin := INF
var moved := false
var leaving := false

func _initialize() -> void:
    _start.call_deferred()

func _start() -> void:
    scene = load("res://scenes/main.tscn").instantiate()
    root.add_child(scene)
    root.add_child(peer)
    scene._connect("ws://127.0.0.1:5080/ws")
    peer.connect_to_server("ws://127.0.0.1:5080/ws")

func _process(delta: float) -> bool:
    elapsed += delta
    if elapsed > 10:
        push_error("Rendered scene did not replicate a real remote avatar and its removal")
        quit(1)
        return false
    if not scene or peer.player_id.is_empty():
        return false
    if scene.avatars.has(peer.player_id):
        var avatar = scene.avatars[peer.player_id]
        if is_inf(remote_origin):
            remote_origin = avatar.position.x
        peer.direction = Vector2.RIGHT
        if avatar.position.x > remote_origin + .6 and not leaving:
            moved = true
            leaving = true
            _finish.call_deferred()
    return false

func _finish() -> void:
    var capture := OS.get_environment("CITY_SCREENSHOT_ONLINE")
    if not capture.is_empty():
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png(capture)
    var remote_id: String = peer.player_id
    peer.disconnect_from_server()
    for i in range(180):
        if not scene.avatars.has(remote_id):
            break
        await physics_frame
    if scene.avatars.has(remote_id) or not moved:
        push_error("Disconnected remote avatar remained in rendered scene")
        quit(1)
        return
    scene.network.disconnect_from_server()
    if not scene.online or scene.avatars.has("local"):
        push_error("Online disconnect silently substituted local training")
        quit(1)
        return
    scene.queue_free()
    peer.queue_free()
    await process_frame
    print("PASS: rendered scene shows real remote humanoid movement, removal and explicit online disconnect")
    quit()
