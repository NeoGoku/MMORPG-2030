extends Node3D
# Original modular buildings. New housing shells are streamed by distance; old Nordtor stays loaded.
var data: Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://data/district.json"))
var chunks: Dictionary={}
var furniture: Dictionary={}
var room_dividers: Dictionary={}
var danger: Dictionary={}
var stalls: Dictionary={}
var last_refresh:=0.0

func box(parent: Node3D,pos: Vector3,scale_value: Vector3,color: Color,collision: bool=false) -> MeshInstance3D:
    var mesh:=MeshInstance3D.new();var shape:=BoxMesh.new();shape.size=scale_value;mesh.mesh=shape
    var material:=StandardMaterial3D.new();material.albedo_color=color;material.roughness=.8;mesh.material_override=material;mesh.position=pos;parent.add_child(mesh)
    if collision:
        var body:=StaticBody3D.new();var collider:=CollisionShape3D.new();var bounds:=BoxShape3D.new();bounds.size=scale_value;collider.shape=bounds;body.add_child(collider);mesh.add_child(body)
    return mesh
func sign_text(parent: Node3D,value: String,pos: Vector3) -> void:
    var label:=Label3D.new();label.text=value;label.position=pos;label.font_size=32;label.pixel_size=.018;label.modulate=Color("eee0bd");parent.add_child(label)
func _ready() -> void:
    box(self,Vector3(36,.12,24),Vector3(61,.12,6),Color("3b4850"))
    box(self,Vector3(28,.12,49),Vector3(5,.12,56),Color("3b4850"))
    for z in [25,45,65]:
        box(self,Vector3(52,.12,z),Vector3(50,.12,3),Color("8c9183"))
    for z in [24,44,64]:
        box(self,Vector3(29,2.1,z),Vector3(.15,4.2,.15),Color("314550"))
        box(self,Vector3(29,4.2,z),Vector3(1,.15,.7),Color("b9e4c5"))
    box(self,Vector3(30,1.2,12),Vector3(3,2.4,.25),Color("32495b"));sign_text(self,"AUKTIONSBÖRSE\n(30,12)",Vector3(30,2.7,12.3))
    sign_text(self,"LINDENHOF →\nWOHNAMT / GILDENREGISTER",Vector3(27,3.5,24))
    sign_text(self,"OFFENE TIEFWERKE\nINDUSTRIEZONE",Vector3(0,4,-45))
    box(self,Vector3(-50,.16,35),Vector3(24,.15,24),Color("796b60"))
    for z in [23,47]:box(self,Vector3(-50,.3,z),Vector3(24,.2,.2),Color("da9b71"))
    for x in [-62,-38]:box(self,Vector3(x,.3,35),Vector3(.2,.2,24),Color("da9b71"))
    sign_text(self,"ARENA · NUR NACH ANNAHME\nNORMIERTE MATCHWERTE",Vector3(-50,3,23))
    for x in [-14,14]:
        for z in [-57,-65,-73,-81]:box(self,Vector3(x,2,z),Vector3(1.2,4,1.2),Color("536b75"),true)
    box(self,Vector3(0,.15,-68),Vector3(29,.2,29),Color("4f6269"))
    sign_text(self,"TIEFWERKE · STUFE 5–8\nAUR-IX: WARNKREIS VERLASSEN / ZUSAMMEN ANGREIFEN",Vector3(0,5,-57))
    sign_text(self,"TRANSIT: STUFE 5 + MIRAS AUFTRAG\nWeltmenü → Arena / Welt",Vector3(0,3,-40))
    box(self,Vector3(0,.12,105),Vector3(6,.15,60),Color("777665"))
    box(self,Vector3(0,.24,91),Vector3(8,.25,8),Color("8b755d"))
    box(self,Vector3(0,.13,127),Vector3(26,.2,22),Color("686f66"))
    for x in [-16,-11,12,18]:
        for z in [98,106,115,124,133]:
            box(self,Vector3(x,1.8,z),Vector3(.7,3.6,.7),Color("675343"))
            box(self,Vector3(x,4,z),Vector3(3.5,3.5,3.5),Color("42664f"))
    for x in [-13,13]:
        for z in [120,129,136]:box(self,Vector3(x,2,z),Vector3(1.4,4,1.4),Color("7c8377"))
    sign_text(self,"GRÜNSAUM · STUFE 10–15\nOffene Wurzelruinen / Wurzelherz",Vector3(0,4,96))
func make_home(def: Dictionary) -> Node3D:
    var chunk:=Node3D.new();chunk.position=Vector3(def.x,0,def.z);add_child(chunk)
    box(chunk,Vector3(0,.14,0),Vector3(18,.25,12),Color("aa9e8b"))
    for wall in [Vector3(-9,1.6,0),Vector3(9,1.6,0),Vector3(0,1.6,-6),Vector3(-7.5,1.6,6),Vector3(3.5,1.6,6)]:
        var dimensions:=Vector3(.4,3.2,12) if absf(wall.x)==9 else Vector3(18 if wall.z<0 else (3 if wall.x<0 else 11),3.2,.4)
        box(chunk,wall,dimensions,Color("b4b3a3"),true)
    for x in [-8.9,8.9]:
        for z in [-3,1,4]:box(chunk,Vector3(x,1.8,z),Vector3(.08,1.2,1.6),Color("607e88"))
    box(chunk,Vector3(-4,3.1,6),Vector3(3,.25,.6),Color("5b8b86"))
    sign_text(chunk,def.name+"\nEINGANG",Vector3(-4,3.8,6.2))
    # Workshop, pet enclosure and roofless courtyard garden occupy real floor coordinates.
    box(chunk,Vector3(-6,1,-4),Vector3(2,.25,1),Color("846548"))
    box(chunk,Vector3(-2,.3,-4),Vector3(2,.5,1.5),Color("6d9068"))
    var divider:=box(chunk,Vector3(0,1.4,0),Vector3(.18,2.8,11.5),Color("596d72"))
    room_dividers[def.id]=divider
    sign_text(chunk,"WERKSTATT · ZUCHT · GARTEN",Vector3(-4,2,-5.7))
    return chunk
func update_world(state: Dictionary,focus: Vector3) -> void:
    var community: Dictionary=state.get("community",{}) if state.get("community") is Dictionary else {}
    for def in data.get("homes",[]):
        var distance:=Vector2(focus.x-def.x,focus.z-def.z).length()
        if distance<60 and not chunks.has(def.id):chunks[def.id]=make_home(def)
        elif distance>75 and chunks.has(def.id):chunks[def.id].queue_free();chunks.erase(def.id);room_dividers.erase(def.id)
    var warned: Dictionary={}
    for enemy in state.get("monsters",[]):
        if enemy.get("warning",0)>0:
            warned[enemy.id]=true
            if not danger.has(enemy.id):
                var circle:=MeshInstance3D.new();var mesh:=CylinderMesh.new();mesh.top_radius=5;mesh.bottom_radius=5;mesh.height=.05;circle.mesh=mesh
                var material:=StandardMaterial3D.new();material.albedo_color=Color(1,.25,.15,.45);material.transparency=BaseMaterial3D.TRANSPARENCY_ALPHA;material.emission_enabled=true;material.emission=Color(.8,.1,.05);circle.material_override=material;add_child(circle);danger[enemy.id]=circle
            danger[enemy.id].position=Vector3(enemy.warningX,.3,enemy.warningZ)
    for id in danger.keys():
        if not warned.has(id):danger[id].queue_free();danger.erase(id)
    var visible_stalls: Dictionary={}
    for stand in community.get("stalls",[]):
        visible_stalls[stand.id]=true
        if not stalls.has(stand.id):
            var node:=Node3D.new();add_child(node);box(node,Vector3(0,.9,0),Vector3(2,.2,1.2),Color("8b7052"));box(node,Vector3(0,2.2,0),Vector3(2.6,.18,1.7),Color("607f89"));sign_text(node,stand.title,Vector3(0,2.8,0));stalls[stand.id]=node
        stalls[stand.id].position=Vector3(stand.x,.2,stand.z)
        for label in stalls[stand.id].get_children():
            if label is Label3D:label.text=stand.title
    for id in stalls.keys():
        if not visible_stalls.has(id):stalls[id].queue_free();stalls.erase(id)
    var seen: Dictionary={}
    for home in community.get("homes",[]):
        if room_dividers.has(home.id):room_dividers[home.id].visible=home.rooms==1 and not home.guildHall
        if not chunks.has(home.id):continue
        var objects=home.get("furniture",[])
        if not objects is Array:continue
        for item in objects:
            seen[item.id]=true
            if not furniture.has(item.id):
                var chair:=Node3D.new();add_child(chair)
                box(chair,Vector3(0,.6,0),Vector3(1.1,.16,1),Color("896347"))
                box(chair,Vector3(0,1.05,.45),Vector3(1.1,.8,.16),Color("a37b51"))
                for x in [-.4,.4]:
                    for z in [-.35,.35]:box(chair,Vector3(x,.3,z),Vector3(.13,.6,.13),Color("514a40"))
                furniture[item.id]=chair
            furniture[item.id].position=Vector3(item.x,.2,item.z);furniture[item.id].rotation.y=item.yaw
    for id in furniture.keys():
        if not seen.has(id):furniture[id].queue_free();furniture.erase(id)
