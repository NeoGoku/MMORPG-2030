extends SceneTree

func _initialize() -> void:
    _run.call_deferred()

func _run() -> void:
    var packed = load("res://scenes/main.tscn") as PackedScene
    var scene = packed.instantiate() if packed else null
    if scene == null or not scene.has_node("Ground") or not scene.has_node("Camera3D") or not scene.has_node("DirectionalLight3D"):
        push_error("Main scene must instantiate with ground, camera and light")
        quit(1)
        return
    root.add_child(scene)
    await process_frame
    await process_frame
    var viewport := root.get_visible_rect()
    for button in scene.touch_buttons:
        if not viewport.encloses(button.get_global_rect()):
            push_error("Touch movement button must be inside viewport: %s" % button.get_global_rect())
            quit(1)
            return
    scene.queue_free()
    await process_frame
    print("PASS: main scene hierarchy, startup and visible touch controls")
    quit()
