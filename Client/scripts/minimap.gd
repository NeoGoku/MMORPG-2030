extends Control
var district: Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://data/district.json"))
var catalog: Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://data/game.json"))
var state: Dictionary={}
var player: Dictionary={}
var radius:=45.0
var destination:=Vector2.INF
func _ready() -> void:
    position=Vector2(25,315);size=Vector2(160,160);mouse_filter=Control.MOUSE_FILTER_IGNORE
func update_world(value: Dictionary,own: Dictionary) -> void:state=value;player=own;queue_redraw()
func point(x: float,z: float) -> Vector2:return Vector2(80,80)+Vector2(x-float(player.get("x",0)),z-float(player.get("z",0)))/radius*72
func _draw() -> void:
    draw_style_box(style(),Rect2(Vector2.ZERO,size));draw_arc(Vector2(80,80),72,0,TAU,48,Color("b8d3c1"),1)
    for group in ["players","monsters"]:
        for entity in state.get(group,[]):
            if group=="players" and int(entity.get("floor",0))!=int(player.get("floor",0)):continue
            var pos:=point(entity.x,entity.z)
            if pos.distance_to(Vector2(80,80))<72:draw_circle(pos,3,Color("f08e74") if group=="monsters" else Color("7eb4dc"))
    for marker in [Vector2(-3,6),Vector2(8,6),Vector2(-8,6),Vector2(30,24),Vector2(30,12)]:
        var pos:=point(marker.x,marker.y)
        if pos.distance_to(Vector2(80,80))<72:draw_circle(pos,3,Color("eac982"))
    for outpost in catalog.get("outposts",[]):
        var pos:=point(outpost.x,outpost.z)
        if pos.distance_to(Vector2(80,80))<72:draw_circle(pos,3,Color("eac982"))
    for passage in district.get("passages",[]):
        for endpoint in [passage.from,passage.to]:
            var pos:=point(endpoint.x,endpoint.z)
            if pos.distance_to(Vector2(80,80))<72:draw_rect(Rect2(pos-Vector2(3,3),Vector2(6,6)),Color("72ded1"))
    draw_circle(Vector2(80,80),4,Color("a7f1cb"))
    if destination!=Vector2.INF:
        var vector:=point(destination.x,destination.y)-Vector2(80,80)
        draw_line(Vector2(80,80),Vector2(80,80)+vector.limit_length(69),Color("fff1a1"),2)
func style() -> StyleBoxFlat:
    var box:=StyleBoxFlat.new();box.bg_color=Color(.03,.09,.12,.85);box.set_corner_radius_all(10);return box
