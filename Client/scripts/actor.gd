extends Node3D

var appearance_key:=""
var worn_weapon: MeshInstance3D
var worn_head: MeshInstance3D
var worn_feet: MeshInstance3D
var worn_accessory: MeshInstance3D
var worn_chest: MeshInstance3D
var kind := "player"
var monster_variant := "drone"
var class_id := "Shield Sword"
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
var skill_effect: MeshInstance3D
var skill_time := 0.0
var _skill_serial := 0
var ring: MeshInstance3D
var timer := 0.0
var attack_time := 0.0
var hurt_time := 0.0
var dead := false
var _attack_serial := 0
var _hurt_serial := 0
var floating: Array = []
static var draw_distance:=60.0
static var materials: Dictionary = {}

func _ready() -> void:
    add_child(model)
    if kind == "pet":
        _part(model,"spirit",SphereMesh.new(),Vector3(0,1.1,0),Vector3(.7,.9,.7),Color("82dccb"),true)
        _part(model,"core",SphereMesh.new(),Vector3(0,1.1,-.3),Vector3(.2,.2,.2),Color("fff2aa"),true)
    elif kind == "monster":
        if monster_variant in ["boar","spirit","tree"]:_creature()
        elif monster_variant=="drone":_drone()
        else:_golem()
    else:
        _human()
    if kind=="player":
        _part(model,"equipped_blade",BoxMesh.new(),Vector3(.65,1.1,-.25),Vector3(.12,1.1,.18),Color("c8d5dc"))
        worn_weapon=model.get_node("equipped_blade");worn_weapon.visible=false
        _part(model,"equipped_chest",BoxMesh.new(),Vector3(0,1.35,0),Vector3(.8,.65,.5),Color("64788d"))
        worn_chest=model.get_node("equipped_chest");worn_chest.visible=false
        _part(model,"equipped_head",BoxMesh.new(),Vector3(0,2.24,0),Vector3(.57,.27,.53),Color("7d8790"));worn_head=model.get_node("equipped_head");worn_head.visible=false
        _part(model,"equipped_feet",BoxMesh.new(),Vector3(0,.16,-.07),Vector3(.65,.25,.48),Color("796652"));worn_feet=model.get_node("equipped_feet");worn_feet.visible=false
        _part(model,"equipped_accessory",SphereMesh.new(),Vector3(0,1.64,-.3),Vector3(.15,.15,.1),Color("a5b9db"),true);worn_accessory=model.get_node("equipped_accessory");worn_accessory.visible=false
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
    skill_effect = MeshInstance3D.new()
    skill_effect.mesh = torus.duplicate()
    skill_effect.material_override = _material(Color("82e5d4"),true)
    skill_effect.position.y = .4
    skill_effect.visible = false
    add_child(skill_effect)
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
    var magic:=class_id in ["Dark Mage","Light Mage","Healer","Creature Summoner"] and kind=="player"
    if magic:
        var robe:=CylinderMesh.new()
        robe.top_radius=.35;robe.bottom_radius=.6;robe.height=1.0
        _part(model,"robe",robe,Vector3(0,.85,.04),Vector3.ONE,tint.darkened(.15))
        _part(sword_arm,"staff",CylinderMesh.new(),Vector3(0,-.1,-.25),Vector3(.12,.9,.12),Color("725f48"))
        _part(sword_arm,"focus",SphereMesh.new(),Vector3(0,.78,-.25),Vector3(.35,.35,.35),Color("9edcd4"),true)
        if class_id=="Dark Mage":_part(model,"hood",SphereMesh.new(),Vector3(0,2.22,.09),Vector3(.6,.4,.55),Color("493b65"))
        if class_id=="Healer":_part(model,"crest",BoxMesh.new(),Vector3(0,1.5,-.24),Vector3(.15,.3,.04),Color("ffe0a2"),true)
    elif class_id=="Archer" and kind=="player":
        var bow:=TorusMesh.new();bow.inner_radius=.35;bow.outer_radius=.41
        _part(sword_arm,"bow",bow,Vector3(0,-.3,-.25),Vector3(.18,1.3,.8),Color("b89b65"))
        sword_arm.get_node("bow").rotation.z=PI/2
        _part(model,"quiver",CylinderMesh.new(),Vector3(.2,1.4,.4),Vector3(.25,.55,.25),Color("6c5344"))
    else:
        _part(sword_arm,"grip",BoxMesh.new(),Vector3(0,-.7,-.1),Vector3(.11,.25,.12),Color("6e5540"))
        _part(sword_arm,"blade",BoxMesh.new(),Vector3(0,-.9,-.43),Vector3(.13,.85,.11),Color("c3e0dc"))
    _part(model,"left_arm",CapsuleMesh.new(),Vector3(-.48,1.32,0),Vector3(.22,.35,.22),tint)
    if class_id=="Dual Swords" and kind=="player":
        _part(model,"second_blade",BoxMesh.new(),Vector3(-.48,.7,-.4),Vector3(.13,.9,.11),Color("c3e0dc"))
    elif not magic and class_id!="Archer":
        _part(model,"shield",BoxMesh.new(),Vector3(-.65,1.18,-.10),Vector3(.13,1.05 if class_id=="Tank" else .75,.7),Color("bca16a"))
        if class_id=="Tank":_part(model,"helmet",BoxMesh.new(),Vector3(0,2.25,.02),Vector3(.58,.25,.53),Color("6b8492"))

func _creature() -> void:
    if monster_variant=="boar":
        _part(model,"body",SphereMesh.new(),Vector3(0,.85,0),Vector3(1.4,.9,2),Color("786353"))
        _part(model,"snout",BoxMesh.new(),Vector3(0,.8,-1),Vector3(.65,.4,.5),Color("a18b74"))
        for side in [-1,1]:
            for z in [-.6,.6]:_part(model,"foot",BoxMesh.new(),Vector3(side*.45,.35,z),Vector3(.25,.7,.25),Color("4c4843"))
            _part(model,"tusk",CylinderMesh.new(),Vector3(side*.3,.9,-1.1),Vector3(.08,.6,.08),Color("ddd0ab"))
    elif monster_variant=="spirit":
        _part(model,"spirit",SphereMesh.new(),Vector3(0,1.3,0),Vector3(.9,1.8,.9),Color("78aaa1"),true)
        for side in [-1,1]:_part(model,"wing",SphereMesh.new(),Vector3(side*.8,1.6,0),Vector3(1,.25,.5),Color("b5d5b8"),true)
    else:
        _part(model,"trunk",CylinderMesh.new(),Vector3(0,1.5,0),Vector3(1.2,3,1.2),Color("6b6650"))
        _part(model,"crown",SphereMesh.new(),Vector3(0,3.4,0),Vector3(3,2.2,2.5),Color("48765f"))
        for side in [-1,1]:_part(model,"root",BoxMesh.new(),Vector3(side*.9,.5,0),Vector3(1.7,.5,1),Color("79745a"))
        _part(model,"heart",SphereMesh.new(),Vector3(0,1.8,-.6),Vector3(.5,.6,.3),Color("b8dd89"),true)

func _golem() -> void:
    var boss: bool=monster_variant=="boss"
    _part(model,"reactor",SphereMesh.new(),Vector3(0,1.5,0),Vector3(1.5,1.3,1.2) if boss else Vector3(1,.9,.8),Color("444f65"))
    _part(model,"core_glow",SphereMesh.new(),Vector3(0,1.6,-.65),Vector3(.55,.55,.35),Color("c578ed") if boss else Color("7bddd0"),true)
    _part(model,"head",BoxMesh.new(),Vector3(0,2.5,0),Vector3(.8,.65,.8),Color("8f9393"))
    for side in [-1,1]:
        _part(model,"arm",BoxMesh.new(),Vector3(side*1.1,1.4,0),Vector3(.5,1.5,.6),Color("747b80"))
        _part(model,"leg",BoxMesh.new(),Vector3(side*.45,.5,0),Vector3(.55,1,.65),Color("526575"))
        if boss:_part(model,"pylon",CylinderMesh.new(),Vector3(side*.8,2.6,.2),Vector3(.3,1.6,.3),Color("d4b28e"))

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
    if kind=="player":
        var value: String=state.get("appearance","default")
        if value!=appearance_key:
            appearance_key=value
            var parts:=value.split("|")
            if parts.size()==6:
                var skin=[Color("d8b8a0"),Color("b78a67"),Color("825c46"),Color("533b32")][int(parts[4])]
                for key in ["head","eyes"]:
                    var node=model.get_node_or_null(key)
                    if node and key=="head":node.material_override=_material(skin,false);node.scale.x=.42+.04*int(parts[1])
                var hair=model.get_node_or_null("hair")
                if hair:hair.scale.y=.18+.07*int(parts[3]);hair.material_override=_material([Color("292a32"),Color("9c744b"),Color("b7bec4"),Color("583d61")][int(parts[3])],false)
                model.scale=Vector3((.94 if parts[0]=="female" else 1.0)+.05*int(parts[2]),.94+.04*int(parts[2]),1)
                var tattoo=model.get_node_or_null("tattoo")
                if tattoo:tattoo.queue_free()
                if int(parts[5])>0:_part(model,"tattoo",BoxMesh.new(),Vector3(.29,1.54,-.23),Vector3(.1,.06*int(parts[5]),.03),Color("3e6e7c"))
        var gear=state.get("equipment",{})
        if gear is Dictionary:
            if worn_weapon:worn_weapon.visible=gear.has("weapon")
            if worn_chest:worn_chest.visible=gear.has("chest")
            if worn_head:worn_head.visible=gear.has("head")
            if worn_feet:worn_feet.visible=gear.has("feet")
            if worn_accessory:worn_accessory.visible=gear.has("accessory")

    target_position = Vector3(state.x, .18+float(state.get("elevation",0)), state.z)
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
    var skill_serial: int = state.get("skillSerial",0)
    if skill_serial>_skill_serial:skill_time=.6
    _skill_serial=skill_serial
    serial = state.get("hurtSerial", 0)
    if serial > _hurt_serial and _hurt_serial >= 0:
        var number := _label("-%d" % state.get("lastDamage", 0), Vector3(0, 2.2, 0), Color("ffac7f"), 42)
        floating.append({"node": number, "life": .9})
        hurt_time = .18
    _hurt_serial = serial

func _process(delta: float) -> void:
    var camera:=get_viewport().get_camera_3d()
    if camera:
        var distance:=global_position.distance_to(camera.global_position)
        model.visible=distance<draw_distance
        name_label.visible=distance<28 and not dead
        hp_label.visible=distance<28 and not dead
    timer += delta
    skill_time = maxf(0,skill_time-delta)
    skill_effect.visible = skill_time>0
    skill_effect.scale = Vector3.ONE*(1.0+(.6-skill_time)*3.0)
    skill_effect.position.y = .4+(.6-skill_time)*2.0
    attack_time = maxf(0, attack_time-delta)
    hurt_time = maxf(0, hurt_time-delta)
    var movement := position.distance_to(target_position)
    position = position.lerp(target_position, minf(1, delta*16))
    model.rotation.y = lerp_angle(model.rotation.y, target_yaw, minf(1,delta*12))
    model.rotation.z = lerpf(model.rotation.z, PI/2 if dead else 0, minf(1,delta*8))
    model.position.y = sin(timer*3)*.08 if kind in ["monster","pet"] and not dead else 0
    if leg_left:
        var stride := sin(timer*12)*.55 if movement > .03 and not dead else 0.0
        leg_left.rotation.x = stride
        leg_right.rotation.x = -stride
        sword_arm.rotation.x = -sin(attack_time/.4*PI)*(1.0 if class_id in ["Archer","Dark Mage","Light Mage","Healer","Creature Summoner"] else 1.8)
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
