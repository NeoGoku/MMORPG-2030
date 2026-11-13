extends Node3D
# Original reusable area scenery; collisions use the same data as the server.
var district: Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://data/district.json"))
var catalog: Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://data/game.json"))
var loaded: Dictionary={}
var batches: Dictionary={}

func box(pos: Vector3,size_value: Vector3,color: String) -> void:
    if not batches.has(color):batches[color]=[]
    batches[color].append(Transform3D(Basis.from_scale(size_value),pos))

func label(parent: Node3D,value: String,pos: Vector3) -> void:
    var sign_node:=Label3D.new();sign_node.text=value;sign_node.position=pos
    sign_node.font_size=30;sign_node.pixel_size=.016;sign_node.modulate=Color("f4dfab")
    parent.add_child(sign_node)

func flush(parent: Node3D) -> void:
    for color in batches:
        var mesh:=MultiMesh.new();mesh.transform_format=MultiMesh.TRANSFORM_3D
        var cube:=BoxMesh.new();cube.size=Vector3.ONE;mesh.mesh=cube
        mesh.instance_count=batches[color].size()
        for i in range(mesh.instance_count):mesh.set_instance_transform(i,batches[color][i])
        var node:=MultiMeshInstance3D.new();node.multimesh=mesh
        var material:=StandardMaterial3D.new();material.albedo_color=Color(color);material.roughness=.8
        node.material_override=material;parent.add_child(node)
    batches.clear()

func _ready() -> void:
    for road in district.get("roads",[]):box(Vector3(road.x,.13,road.z),Vector3(road.width,.1,road.depth),"43535a")
    flush(self)
    for passage in district.get("passages",[]):
        for endpoint in [passage.from,passage.to]:
            box(Vector3(endpoint.x,.23,endpoint.z),Vector3(2,.15,2),"67a7a2")
            label(self,"%s\nSTUFE %d · WELTMENÜ"%[passage.name,passage.minLevel],Vector3(endpoint.x,3,endpoint.z))
    flush(self)

func make_area(area: Dictionary) -> Node3D:
    var node:=Node3D.new();node.position=Vector3(area.x,0,area.z);add_child(node)
    var industrial: bool=area.theme=="industrial"
    box(Vector3(0,.12,0),Vector3(area.radius*1.8,.15,area.radius*1.8),"616f73" if industrial else "777064")
    # Facade/courtyard detail stays within the area, leaving walking paths open.
    for x in [-20,20]:
        for z in [-18,-6,6,18]:
            if industrial:
                box(Vector3(x,2.4,z),Vector3(2,4.8,2),"526670")
                box(Vector3(x,4.9,z),Vector3(2.4,.2,2.4),"9fbea9")
                for y in [1,2,3]:box(Vector3(x-.01,y,z+1.02),Vector3(1.6,.3,.04),"86aeb3")
            else:
                box(Vector3(x,2,z),Vector3(.6,4,.6),"625047")
                box(Vector3(x,4,z),Vector3(3.2,2.7,3.2),"766858")
                box(Vector3(x+.7,5.5,z),Vector3(2,1.4,2),"a27953")
    for wall in district.get("walls",[]):
        if Vector2(wall.x-area.x,wall.z-area.z).length()>area.radius:continue
        var position_value:=Vector3(wall.x-area.x,1.8,wall.z-area.z)
        box(position_value,Vector3(wall.width,3.6,wall.depth),"819294" if industrial else "8e8774")
        var body:=StaticBody3D.new();body.position=position_value;node.add_child(body)
        var collider:=CollisionShape3D.new();var shape:=BoxShape3D.new();shape.size=Vector3(wall.width,3.6,wall.depth)
        collider.shape=shape;body.add_child(collider)
    for npc in catalog.get("outposts",[]):
        if Vector2(npc.x-area.x,npc.z-area.z).length()>area.radius+5:continue
        var position_value:=Vector3(npc.x-area.x,0,npc.z-area.z)
        box(position_value+Vector3(0,1,0),Vector3(.65,1.3,.4),"496b7b" if industrial else "836c56")
        box(position_value+Vector3(0,1.85,0),Vector3(.4,.45,.4),"c7ab8b")
        for x in [-.18,.18]:box(position_value+Vector3(x,.3,0),Vector3(.2,.6,.25),"3f4e56")
        label(node,npc.name+"\nInventar / Questlog / Handwerk",position_value+Vector3(0,2.7,0))
    label(node,area.name,Vector3(0,5,area.radius*.7))
    flush(node)
    return node

func update_focus(focus: Vector3) -> void:
    for area in district.get("areas",[]):
        var distance:=Vector2(focus.x-area.x,focus.z-area.z).length()
        if distance<area.radius+55 and not loaded.has(area.id):loaded[area.id]=make_area(area)
        elif distance>area.radius+70 and loaded.has(area.id):loaded[area.id].queue_free();loaded.erase(area.id)
