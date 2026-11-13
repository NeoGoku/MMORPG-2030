extends Panel
signal chosen(value: String)
const Actor=preload("res://scripts/actor.gd")
const TouchAction=preload("res://scripts/touch_action.gd")
var selectors: Array[OptionButton]=[]
var preview: Actor
var class_id:="Shield Sword"
func _ready() -> void:
    position=Vector2(150,55);size=Vector2(980,610);z_index=40
    var background:=StyleBoxFlat.new();background.bg_color=Color("102b36");background.set_corner_radius_all(12);add_theme_stylebox_override("panel",background)
    var title:=Label.new();title.text="CHARAKTERVORSCHAU · ONLINE-ERSTELLUNG";title.position=Vector2(24,18);title.add_theme_font_size_override("font_size",22);add_child(title)
    var groups: Array=[["neutral","male","female"],["Gesicht 1","Gesicht 2","Gesicht 3","Gesicht 4"],["Körper 1","Körper 2","Körper 3","Körper 4"],["Frisur 1","Frisur 2","Frisur 3","Frisur 4"],["Haut 1","Haut 2","Haut 3","Haut 4"],["Kein Tattoo","Tattoo 1","Tattoo 2","Tattoo 3"]]
    for index in range(groups.size()):
        var choose:=OptionButton.new();choose.position=Vector2(24,65+index*62);choose.size=Vector2(400,48)
        for value in groups[index]:choose.add_item(value)
        choose.item_selected.connect(func(_value):update_preview());selectors.append(choose);add_child(choose)
    var container:=SubViewportContainer.new();container.position=Vector2(450,60);container.size=Vector2(500,460);container.stretch=true;add_child(container)
    var viewport:=SubViewport.new();viewport.size=Vector2i(500,460);viewport.own_world_3d=true;viewport.render_target_update_mode=SubViewport.UPDATE_WHEN_VISIBLE;container.add_child(viewport)
    preview=Actor.new();preview.kind="player";preview.class_id=class_id;preview.display_name="Vorschau";viewport.add_child(preview)
    var camera:=Camera3D.new();camera.position=Vector3(0,1.5,-4.8);viewport.add_child(camera);camera.look_at(Vector3(0,1.35,0));camera.current=true
    var light:=DirectionalLight3D.new();light.rotation_degrees=Vector3(-35,-25,0);light.light_energy=1.5;viewport.add_child(light)
    var world:=WorldEnvironment.new();var environment:=Environment.new();environment.background_mode=Environment.BG_COLOR;environment.background_color=Color("223b44");environment.ambient_light_source=Environment.AMBIENT_SOURCE_COLOR;environment.ambient_light_color=Color.WHITE;environment.ambient_light_energy=.6;world.environment=environment;viewport.add_child(world)
    var accept=TouchAction.new();accept.text="Auswahl für Erstellung übernehmen";accept.position=Vector2(24,530);accept.size=Vector2(650,50);accept.pressed.connect(func():chosen.emit(value());hide());add_child(accept)
    var close=TouchAction.new();close.text="Zurück";close.position=Vector2(700,530);close.size=Vector2(250,50);close.pressed.connect(hide);add_child(close)
    update_preview();hide()
func value() -> String:
    return "%s|%d|%d|%d|%d|%d"%[selectors[0].get_item_text(selectors[0].selected),selectors[1].selected,selectors[2].selected,selectors[3].selected,selectors[4].selected,selectors[5].selected]
func update_preview() -> void:
    if preview:preview.apply_state({"appearance":value(),"x":0,"z":0,"yaw":0,"hp":100,"maxHp":100,"equipment":{}},false)
func open_for_class(selected_class: String) -> void:
    if selected_class!=class_id:
        class_id=selected_class
        # Rebuild the preview actor once when changing class, preserving the selected face/body values.
        if preview:
            var parent=preview.get_parent();parent.remove_child(preview);preview.queue_free();preview=Actor.new();preview.kind="player";preview.class_id=class_id;preview.display_name="Vorschau";parent.add_child(preview);update_preview()
    show()
