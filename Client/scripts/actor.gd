extends Node3D

var kind := "player"
var tint := Color("39cbb3")
var display_name := "Reisender"
var target_position := Vector3.ZERO
var target_yaw := 0.0
var model := Node3D.new()
var leg_left: Node3D
var leg_right: Node3D
var sword_arm: Node3D
var name_label: Label3D
var hp_label: Label3D
var ring: MeshInstance3D
var timer := 0.0
var attack_time := 0.0
var hurt_time := 0.0
var dead := false
var _attack_serial := 0
var _hurt_serial := 0
var floating: Array = []
static var materials: Dictionary = {}

func _ready() -> void:
    add_child(model)
    if kind == "monster":
        _drone()
    else:
        _human()
    name_label = _label(display_name, Vector3(0, 2.65, 0), Color("e7eee3"), 26)
    hp_label = _label("", Vector3(0, 2.35, 0), tint, 22)
    ring = MeshInstance3D.new()
    var torus := TorusMesh.new()
    torus.inner_radius = .65
    torus.outer_radius = .73
    torus.rings = 24
    torus.ring_segments = 8
    ring.mesh = torus
    ring.material_override = _material(Color("f3c671"), true)
    ring.position.y = .12
    ring.visible = kind == "npc"
    add_child(ring)
    if kind == "npc":
        _label("!", Vector3(0, 3.3, 0), Color("ffda7b"), 52)

func _human() -> void:
    _part(model, "torso", CapsuleMesh.new(), Vector3(0, 1.35, 0), Vector3(.65, .52, .42), tint)
    _part(model, "belt", BoxMesh.new(), Vector3(0, .96, 0), Vector3(.72, .15, .45), Color("293e4c"))
    _part(model, "head", SphereMesh.new(), Vector3(0, 2.05, 0), Vector3(.48, .48, .48), Color("ccac88"))
    _part(model, "hair", SphereMesh.new(), Vector3(0, 2.18, .04), Vector3(.52, .28, .50), Color("2b3640"))
    _part(model, "eyes", BoxMesh.new(), Vector3(0, 2.08, -.23), Vector3(.25, .045, .035), Color("183b48"))
    _part(model, "collar", BoxMesh.new(), Vector3(0, 1.7, 0), Vector3(.76, .17, .48), Color("eee0b5"))
    for side in [-1, 1]:
        var leg := Node3D.new()
        leg.position = Vector3(side*.20, .95, 0)
        model.add_child(leg)
        _part(leg, "leg", CapsuleMesh.new(), Vector3(0, -.38, 0), Vector3(.22, .37, .24), Color("344e5b"))
        _part(leg, "boot", BoxMesh.new(), Vector3(0, -.78, -.07), Vector3(.29, .24, .43), Color("25343d"))
        if side == -1:
            leg_left = leg
        else:
            leg_right = leg
    sword_arm = Node3D.new()
    sword_arm.position = Vector3(.48, 1.65, 0)
    model.add_child(sword_arm)
    _part(sword_arm, "arm", CapsuleMesh.new(), Vector3(0, -.3, 0), Vector3(.22, .35, .22), tint)
    _part(sword_arm, "grip", BoxMesh.new(), Vector3(0, -.7, -.1), Vector3(.11, .25, .12), Color("6e5540"))
    _part(sword_arm, "guard", BoxMesh.new(), Vector3(0, -.58, -.12), Vector3(.42, .10, .18), Color("d8bc7c"))
    _part(sword_arm, "blade", BoxMesh.new(), Vector3(0, -.9, -.43), Vector3(.13, .85, .11), Color("c3e0dc"))
    _part(model, "left_arm", CapsuleMesh.new(), Vector3(-.48, 1.32, 0), Vector3(.22, .35, .22), tint)
    _part(model, "shield", BoxMesh.new(), Vector3(-.65, 1.18, -.10), Vector3(.13, .75, .58), Color("bca16a"))
    _part(model, "shield_core", BoxMesh.new(), Vector3(-.73, 1.18, -.10), Vector3(.05, .48, .32), tint)

func _drone() -> void:
    _part(model, "core", SphereMesh.new(), Vector3(0, 1.0, 0), Vector3(1.15, .7, .9), Color("92694e"))
    _part(model, "visor", BoxMesh.new(), Vector3(0, 1.03, -.42), Vector3(.70, .15, .10), Color("ff7651"), true)
    for side in [-1, 1]:
        _part(model, "arm", BoxMesh.new(), Vector3(side*.8, 1.15, 0), Vector3(.65, .15, .2), Color("35434b"))
        _part(model, "rotor", CylinderMesh.new(), Vector3(side*1.02, 1.25, 0), Vector3(.9, .06, .9), Color("263e45"))
        _part(model, "rotor_glow", BoxMesh.new(), Vector3(side*1.02, 1.31, 0), Vector3(.7, .02, .08), Color("e8ab62"), true)

func _material(color: Color, glow: bool) -> StandardMaterial3D:
    var key := color.to_html() + str(glow)
    if not materials.has(key):
        var material := StandardMaterial3D.new()
        material.albedo_color = color
        material.roughness = .65
        if glow:
            material.emission_enabled = true
            material.emission = color
        materials[key] = material
    return materials[key]

func _part(parent: Node3D, title: String, mesh: Mesh, pos: Vector3, size: Vector3, color: Color, glow := false) -> void:
    var part := MeshInstance3D.new()
    part.name = title
    part.mesh = mesh
    part.position = pos
    part.scale = size
    part.material_override = _material(color, glow)
    parent.add_child(part)

func _label(text: String, pos: Vector3, color: Color, font_size: int) -> Label3D:
    var label := Label3D.new()
    label.text = text
    label.font_size = font_size
    label.pixel_size = .008
    label.billboard = BaseMaterial3D.BILLBOARD_ENABLED
    label.modulate = color
    label.outline_size = 6
    label.position = pos
    add_child(label)
    return label

func apply_state(state: Dictionary, selected: bool) -> void:
    target_position = Vector3(state.x, .18, state.z)
    if state.has("yaw"):
        target_yaw = state.yaw
    dead = state.get("dead", false)
    name_label.visible = not dead
    hp_label.text = "%d / %d HP" % [state.hp, state.maxHp] if not dead else ""
    ring.visible = selected and not dead
    var serial: int = state.get("attackSerial", 0)
    if serial > _attack_serial:
        attack_time = .4
    _attack_serial = serial
    serial = state.get("hurtSerial", 0)
    if serial > _hurt_serial and _hurt_serial >= 0:
        var number := _label("-%d" % state.get("lastDamage", 0), Vector3(0, 2.2, 0), Color("ffac7f"), 42)
        floating.append({"node": number, "life": .9})
        hurt_time = .18
    _hurt_serial = serial

func _process(delta: float) -> void:
    timer += delta
    attack_time = maxf(0, attack_time-delta)
    hurt_time = maxf(0, hurt_time-delta)
    var movement := position.distance_to(target_position)
    position = position.lerp(target_position, minf(1, delta*16))
    model.rotation.y = lerp_angle(model.rotation.y, target_yaw, minf(1,delta*12))
    model.rotation.z = lerpf(model.rotation.z, PI/2 if dead else 0, minf(1,delta*8))
    model.position.y = sin(timer*3)*.08 if kind == "monster" and not dead else 0
    if leg_left:
        var stride := sin(timer*12)*.55 if movement > .03 and not dead else 0.0
        leg_left.rotation.x = stride
        leg_right.rotation.x = -stride
        sword_arm.rotation.x = -sin(attack_time/.4*PI)*1.8
        sword_arm.rotation.z = -sin(attack_time/.4*PI)*.8
    if kind == "monster":
        model.rotation.x = sin(attack_time/.4*PI)*.35
    for entry in floating.duplicate():
        entry.life -= delta
        entry.node.position.y += delta
        entry.node.modulate.a = maxf(0, entry.life/.9)
        if entry.life <= 0:
            entry.node.queue_free()
            floating.erase(entry)
