extends SceneTree
func _initialize() -> void:call_deferred("run")
func run() -> void:
    var city=load("res://scripts/community_city.gd").new();root.add_child(city)
    await process_frame
    var home: Dictionary=city.data.homes[0].duplicate(true)
    home.merge({"owner":"test","rooms":2,"floors":3,"floorHeight":3.6,"furniture":[{"id":"upper-chair","item":"oak_chair","floor":1,"x":36,"z":32,"yaw":0.0}]},true)
    var state={"self":{"character":{"floor":1}},"community":{"homes":[home]}}
    city.update_world(state,Vector3(33,3.6,37))
    assert(city.floor_chunks.has("home-1|1") and city.floor_chunks.has("home-1|2"))
    assert(city.floor_chunks["home-1|1"].visible and not city.floor_chunks["home-1|2"].visible)
    assert(is_equal_approx(city.furniture["upper-chair"].position.y,3.8))
    var actor=load("res://scripts/actor.gd").new();root.add_child(actor)
    actor.apply_state({"x":33,"z":37,"elevation":3.6,"hp":100,"maxHp":100,"dead":false},false)
    assert(is_equal_approx(actor.target_position.y,3.78))
    city.update_world(state,Vector3(-90,0,-90))
    assert(city.floor_chunks.is_empty())
    actor.queue_free();city.queue_free();await process_frame
    print("PASS M8: shared upper-floor cutaway, furniture/actor elevation and distance unload")
    quit()
