extends Panel
signal quality_changed(distance: float,shadows: bool)
const TouchAction=preload("res://scripts/touch_action.gd")
var profile:=1
func _ready() -> void:
    position=Vector2(330,150);size=Vector2(620,360);z_index=30
    var style:=StyleBoxFlat.new();style.bg_color=Color("102b36");style.set_corner_radius_all(12);add_theme_stylebox_override("panel",style)
    var label:=Label.new();label.text="DARSTELLUNG / LEISTUNG";label.position=Vector2(24,24);label.add_theme_font_size_override("font_size",24);add_child(label)
    var y:=80
    for preset in ["Sparsam · 30 FPS / 75 % Auflösung / kurze Sicht","Ausgewogen · 60 FPS / volle Auflösung","Weite Sicht · 60 FPS / Schatten"]:
        var index: int=(y-80)/58
        var button=TouchAction.new();button.text=preset;button.position=Vector2(20,y);button.size=Vector2(580,48);button.pressed.connect(func():apply_profile(index));add_child(button);y+=58
    var close=TouchAction.new();close.text="Schließen";close.position=Vector2(20,280);close.size=Vector2(580,48);close.pressed.connect(hide);add_child(close)
    var config:=ConfigFile.new()
    if config.load("user://graphics.cfg")==OK:profile=clampi(int(config.get_value("graphics","profile",1)),0,2)
    apply_profile.call_deferred(profile);hide()
func apply_profile(index: int) -> void:
    profile=clampi(index,0,2);Engine.max_fps=30 if profile==0 else 60
    get_viewport().scaling_3d_scale=.75 if profile==0 else 1.0
    quality_changed.emit([35.0,60.0,90.0][profile],profile==2)
    var config:=ConfigFile.new();config.set_value("graphics","profile",profile);config.save("user://graphics.cfg")
