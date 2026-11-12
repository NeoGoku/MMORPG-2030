extends Node3D

# All city assets are authored here from original geometry; grouped into material batches.
var data: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://data/district.json"))
var scene_environment: Environment
var sky_colors: ProceduralSkyMaterial
var rain: CPUParticles3D
var batches: Dictionary = {}
var materials: Dictionary = {}

func _ready() -> void:
    _box(Vector3(0, -.25, 0), Vector3(116, .5, 116), Color("67736b"))
    _box(Vector3(0, .015, 0), Vector3(15, .12, 112), Color("b7b5a5"))
    _box(Vector3(0, .09, 0), Vector3(10, .08, 112), Color("35434b"))
    _box(Vector3(0, .10, 1), Vector3(65, .08, 7), Color("35434b"))
    for z in range(-50, 57, 5):
        if abs(z - 1) > 5:
            _box(Vector3(0, .16, z), Vector3(.12, .015, 2), Color("e1d9b3"))
    for x in [-5.0, 5.0]:
        _box(Vector3(x, .17, 0), Vector3(.15, .15, 112), Color("e2dbca"))
    for x in range(-5, 6, 2):
        _box(Vector3(x, .16, 3), Vector3(.7, .02, 2), Color("e5dfcb"))
    # A welcoming stone plaza and the northern drone yard.
    _box(Vector3(-4, .15, 7), Vector3(5, .14, 6), Color("c5bda7"))
    _box(Vector3(0, .16, -14), Vector3(22, .10, 22), Color("637273"))
    for x in range(-10, 11, 2):
        _box(Vector3(x, .23, -3.3), Vector3(.9, .02, .5), Color("e9b75f"))
    for index in range(data.buildings.size()):
        _building(data.buildings[index], index)
    # Street lamps, benches and trees use the same batched meshes as the buildings.
    for z in [-25, -13, 8, 24]:
        for x in [-6.7, 6.7]:
            _box(Vector3(x, 2.5, z), Vector3(.12, 5, .12), Color("263c45"))
            _box(Vector3(x, 5, z), Vector3(1, .15, .55), Color("dfecca"))
            _box(Vector3(x, 4.85, z), Vector3(.8, .09, .4), Color("6fcdbb"), true)
    for x in [-9.5, 9.5]:
        for z in [8, 24, -17]:
            _box(Vector3(x, .26, z), Vector3(2.4, .5, 2.4), Color("b5a58b"))
            _box(Vector3(x, 1.5, z), Vector3(.32, 2.5, .32), Color("655447"))
            _box(Vector3(x, 3.2, z), Vector3(2.6, 2, 2.6), Color("45685a"))
            _box(Vector3(x + .3, 4.2, z), Vector3(1.8, 1, 1.8), Color("68836a"))
    for x in [-8.0, 8.0]:
        _box(Vector3(x, .8, 17), Vector3(2.2, .18, .65), Color("8d6950"))
        _box(Vector3(x, 1.1, 17.3), Vector3(2.2, .7, .12), Color("8d6950"))
        _box(Vector3(x-.7, .4, 17), Vector3(.15, .7, .5), Color("263c45"))
        _box(Vector3(x+.7, .4, 17), Vector3(.15, .7, .5), Color("263c45"))
    # Gate visually separates the safe plaza from the combat yard.
    for x in [-6, 6]:
        _box(Vector3(x, 2.5, -3), Vector3(.55, 5, .55), Color("344d55"))
        _box(Vector3(x, 2.5, -2.7), Vector3(.14, 3.8, .08), Color("65dcc4"), true)
    _box(Vector3(0, 5, -3), Vector3(12.6, 1.05, .6), Color("233c47"))
    _sign("N O R D T O R  /  2 0 3 0", Vector3(0, 5.03, -2.65), .025, Color("e7e4c9"))
    _sign("NORDHOF  ·  DROHNENSPERRZONE", Vector3(0, .3, -5.2), .013, Color("edc37c"), Vector3(-PI/2, 0, 0))
    # District perimeter and distant skyline (no areas beyond the playable boundary).
    for x in [-57, 57]:
        _box(Vector3(x, 1, 0), Vector3(.5, 2, 114), Color("89958a"))
    for z in [-57, 57]:
        _box(Vector3(0, 1, z), Vector3(114, 2, .5), Color("89958a"))
    for i in range(12):
        var x: float = -46 + i * 8.5
        var height: float = 12 + (i * 7 % 15)
        _box(Vector3(x, height/2, -47), Vector3(6, height, 7), Color("73888a"))
        _box(Vector3(x, height+.5, -47), Vector3(1, 1, 1), Color("82c5bb"), true)
    for x in [-8,8]:
        for z in [35,41,49]:
            _box(Vector3(x,1.5,z),Vector3(.3,3,.3),Color("655447"))
            _box(Vector3(x,3.2,z),Vector3(3,2,3),Color("45685a"))
    _sign("S Ü D P A R K",Vector3(0,3.5,33),.035,Color("edc67e"))
    _flush()
    var environment_node := WorldEnvironment.new()
    var environment := Environment.new()
    var sky := Sky.new()
    var sky_material := ProceduralSkyMaterial.new()
    sky_material.sky_top_color = Color("52879d")
    sky_material.sky_horizon_color = Color("d5d6bd")
    sky_material.ground_bottom_color = Color("59685f")
    sky_material.ground_horizon_color = Color("d5d6bd")
    sky.sky_material = sky_material
    environment.background_mode = Environment.BG_SKY
    environment.sky = sky
    environment.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
    environment.ambient_light_color = Color("b5c9c5")
    environment.ambient_light_energy = .45
    environment.tonemap_mode = Environment.TONE_MAPPER_FILMIC
    environment.tonemap_white = 6.0
    environment_node.environment = environment
    add_child(environment_node)
    scene_environment=environment
    sky_colors=sky_material
    rain=CPUParticles3D.new()
    rain.amount=96
    rain.lifetime=1.5
    rain.emitting=false
    rain.emission_shape=CPUParticles3D.EMISSION_SHAPE_BOX
    rain.emission_box_extents=Vector3(12,.5,12)
    rain.direction=Vector3.DOWN
    rain.spread=4
    rain.gravity=Vector3(0,-16,0)
    rain.initial_velocity_min=8
    rain.initial_velocity_max=12
    var streak:=BoxMesh.new()
    streak.size=Vector3(.025,.6,.025)
    rain.mesh=streak
    var water:=StandardMaterial3D.new()
    water.albedo_color=Color(.6,.8,.95,.5)
    water.transparency=BaseMaterial3D.TRANSPARENCY_ALPHA
    rain.material_override=water
    add_child(rain)

func _building(b: Dictionary, index: int) -> void:
    var colors := [Color("b3aa93"), Color("999d91"), Color("9cabaa")]
    _box(Vector3(b.x, b.height/2, b.z), Vector3(b.width, b.height, b.depth), colors[index%3])
    _box(Vector3(b.x, 1, b.z), Vector3(b.width+.15, 2, b.depth+.15), Color("526568"))
    _box(Vector3(b.x, b.height+.22, b.z), Vector3(b.width+.5, .45, b.depth+.5), Color("455c61"))
    _box(Vector3(b.x, b.height+.7, b.z-1), Vector3(2.5, 1, 3), Color("8a9b92"))
    for y in range(4, int(b.height)-1, 3):
        for x in range(int(-b.width/2)+2, int(b.width/2), 3):
            _box(Vector3(b.x+x, y, b.z+b.depth/2+.04), Vector3(1.3, 1.6, .1), Color("304c58"))
            _box(Vector3(b.x+x, y+.65, b.z+b.depth/2+.11), Vector3(1.1, .1, .05), Color("8cb9b4"), true)
    _box(Vector3(b.x, 1.25, b.z+b.depth/2+.12), Vector3(1.5, 2.5, .15), Color("213a43"))
    _box(Vector3(b.x, 3.2, b.z+b.depth/2+.3), Vector3(minf(b.width-1, 8), .9, .5), Color("263c45"))
    _sign(b.name, Vector3(b.x, 3.2, b.z+b.depth/2+.58), .020, Color("f0dbb0"))
    var body := StaticBody3D.new()
    var shape := CollisionShape3D.new()
    var box := BoxShape3D.new()
    box.size = Vector3(b.width, b.height, b.depth)
    shape.shape = box
    body.position = Vector3(b.x, b.height/2, b.z)
    body.add_child(shape)
    add_child(body)

func _sign(text: String, pos: Vector3, pixel: float, color: Color, rot := Vector3.ZERO) -> void:
    var label := Label3D.new()
    label.text = text
    label.font_size = 36
    label.pixel_size = pixel
    label.modulate = color
    label.outline_size = 4
    label.position = pos
    label.rotation = rot
    add_child(label)

func _box(pos: Vector3, size: Vector3, color: Color, glow := false) -> void:
    var key := color.to_html() + str(glow)
    if not batches.has(key):
        batches[key] = []
        var material := StandardMaterial3D.new()
        material.albedo_color = color
        material.roughness = .85
        if glow:
            material.emission_enabled = true
            material.emission = color
            material.emission_energy_multiplier = .8
        materials[key] = material
    batches[key].append(Transform3D(Basis().scaled(size), pos))

func _flush() -> void:
    for key in batches:
        var multi := MultiMesh.new()
        multi.transform_format = MultiMesh.TRANSFORM_3D
        var cube := BoxMesh.new()
        cube.size = Vector3.ONE
        multi.mesh = cube
        multi.instance_count = batches[key].size()
        for i in range(multi.instance_count):
            multi.set_instance_transform(i, batches[key][i])
        var instance := MultiMeshInstance3D.new()
        instance.multimesh = multi
        instance.material_override = materials[key]
        add_child(instance)
    batches.clear()

func apply_environment(state: Dictionary,focus: Vector3) -> void:
    if not scene_environment:return
    var phase: float=state.get("phase",0)
    var daylight:=maxf(0,cos(phase*TAU))
    var weather: String=state.get("weather","klar")
    var light: DirectionalLight3D=get_parent().get_node("DirectionalLight3D")
    light.light_energy=.12+daylight*.9
    light.rotation_degrees=Vector3(-10-daylight*50,phase*360-28,0)
    scene_environment.ambient_light_energy=.24+daylight*.22
    sky_colors.sky_top_color=Color("132337").lerp(Color("52879d"),daylight)
    if weather=="Regen":sky_colors.sky_top_color=sky_colors.sky_top_color.darkened(.35)
    scene_environment.fog_enabled=weather!="klar"
    scene_environment.fog_density=.018 if weather=="Nebel" else .006
    scene_environment.fog_light_color=Color("7f9a9e")
    rain.position=focus+Vector3(0,12,0)
    rain.emitting=weather=="Regen"
