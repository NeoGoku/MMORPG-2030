extends SceneTree

func _initialize() -> void:
    _run.call_deferred()

func check(value: bool, text: String) -> bool:
    if not value:
        push_error(text)
        quit(1)
    return value

func _run() -> void:
    var packed = load("res://scenes/main.tscn") as PackedScene
    var scene = packed.instantiate() if packed else null
    if not check(scene != null and scene.has_node("City") and scene.has_node("DirectionalLight3D"), "Main must instantiate with city and light"):
        return
    root.add_child(scene)
    for i in range(8):
        await process_frame
    var viewport := root.get_visible_rect()
    for button in scene.hud.action_buttons:
        if not check(viewport.encloses(button.get_global_rect()), "Action button must be inside viewport: %s" % button.get_global_rect()):
            return
    if not check(scene.avatars.has("local") and scene.monsters.size() == 3, "Immediate offline player and visible monsters"):
        return
    if not check(scene.avatars.local.model.has_node("torso") and scene.avatars.local.model.has_node("head"), "Visible articulated humanoid"):
        return
    var projected: Vector2 = scene.camera.unproject_position(scene.avatars.local.position + Vector3(0,1,0))
    if not check(viewport.has_point(projected) and not scene.camera.is_position_behind(scene.avatars.local.position), "Third-person character in camera view"):
        return
    # One finger holds the joystick while another taps an action.
    var joystick = scene.hud.joystick
    var down := InputEventScreenTouch.new()
    down.index = 0
    down.pressed = true
    down.position = joystick.get_global_rect().get_center() + Vector2(40, -20)
    var initial_x: float = scene.practice.player.x
    Input.parse_input_event(down)
    for i in range(18):
        await physics_frame
    if not check(scene.practice.player.x > initial_x + .05, "Analog joystick actually moves the character"):
        return
    if not check(joystick.direction.length() > .5, "Analog joystick accepts touch"):
        return
    var action := InputEventScreenTouch.new()
    action.index = 1
    action.pressed = true
    action.position = scene.hud.interact_button.get_global_rect().get_center()
    var taps := [0]
    scene.hud.action_requested.connect(func(_kind: String): taps[0] += 1)
    Input.parse_input_event(action)
    await process_frame
    if not check(joystick.pointer == 0, "Action touch preserves movement finger"):
        return
    action.pressed = false
    Input.parse_input_event(action)
    await process_frame
    if not check(taps[0] == 1, "Multi-touch action fires exactly once"):
        return
    down.pressed = false
    Input.parse_input_event(down)
    await process_frame
    if not check(joystick.direction == Vector2.ZERO, "Joystick releases to neutral"):
        return
    action.index = 0
    action.pressed = true
    Input.parse_input_event(action)
    await process_frame
    action.pressed = false
    Input.parse_input_event(action)
    await process_frame
    if not check(taps[0] == 2, "Single-touch action not duplicated by mouse emulation"):
        return
    var capture := OS.get_environment("CITY_SCREENSHOT")
    if not capture.is_empty():
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png(capture)
    scene.queue_free()
    await process_frame
    print("PASS: city startup, humanoid/camera, visible mobile HUD and independent touch inputs")
    quit()
