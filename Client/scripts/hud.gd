extends Control

signal action_requested(kind: String)
signal connect_requested(url: String)
signal practice_requested

const Joystick = preload("res://scripts/joystick.gd")
const TouchAction = preload("res://scripts/touch_action.gd")
var joystick = Joystick.new()
var hp: ProgressBar
var xp: ProgressBar
var player_title: Label
var hp_text: Label
var mode_label: Label
var quest: Label
var message: Label
var target: Label
var inventory: Label
var objective: Label
var attack_button: Button
var interact_button: Button
var loot_button: Button
var online_panel: Panel
var endpoint: LineEdit
var action_buttons: Array[Button] = []
var _last_message := ""
var _message_time := 0.0

func _ready() -> void:
    set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
    mouse_filter = Control.MOUSE_FILTER_IGNORE
    var theme_resource := Theme.new()
    theme_resource.default_font_size = 20
    theme = theme_resource
    var profile := _panel(Rect2(22, 20, 310, 120))
    player_title = _text(profile, "Reisender  /  Stufe 1", Vector2(18, 10), 22)
    hp = _bar(profile, Rect2(18, 45, 274, 22), Color("65c19a"))
    hp_text = _text(profile, "100 / 100 HP", Vector2(28, 45), 16)
    xp = _bar(profile, Rect2(18, 77, 274, 8), Color("dfba71"))
    mode_label = _text(profile, "LOKALES TRAINING · NICHT GESPEICHERT", Vector2(18, 95), 12, Color("b5cac6"))
    var district := _text(self, "N O R D T O R", Vector2(0, 24), 28)
    _anchor(district, Control.PRESET_TOP_RIGHT, Vector2(-262, 24))
    var subtitle := _text(self, "STADTWACHE  /  SEKTOR 03", Vector2.ZERO, 13, Color("b6d7cf"))
    _anchor(subtitle, Control.PRESET_TOP_RIGHT, Vector2(-262, 62))
    var online := _button(self, "Online", Rect2(0, 0, 124, 42), "online")
    _anchor(online, Control.PRESET_TOP_RIGHT, Vector2(-148, 90))
    online.pressed.connect(func(): online_panel.visible = not online_panel.visible)
    var task_panel := _panel(Rect2(0, 0, 330, 140))
    _anchor(task_panel, Control.PRESET_TOP_RIGHT, Vector2(-354, 155))
    _text(task_panel, "AUFTRAG  /  WACHE", Vector2(16, 12), 14, Color("edc67e"))
    quest = _text(task_panel, "Ein sicherer Bezirk", Vector2(16, 39), 22)
    objective = _text(task_panel, "Sprich mit Mira bei der Stadtwache.", Vector2(16, 74), 17)
    objective.size = Vector2(295, 60)
    objective.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    inventory = _text(self, "0 Münzen   ·   0 Schrott", Vector2.ZERO, 16, Color("e2d6b4"))
    _anchor(inventory, Control.PRESET_TOP_RIGHT, Vector2(-354, 308))
    target = _text(self, "", Vector2.ZERO, 20, Color("ffcd91"))
    _anchor(target, Control.PRESET_CENTER_TOP, Vector2(-200, 30))
    target.size = Vector2(400, 60)
    target.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
    # Anchor all touch UI to the landscape viewport, with generous hit areas.
    add_child(joystick)
    _anchor(joystick, Control.PRESET_BOTTOM_LEFT, Vector2(24, -226))
    joystick.size = Vector2(210, 210)
    var move_hint := _text(self, "BEWEGEN", Vector2.ZERO, 13, Color("b8d4ce"))
    _anchor(move_hint, Control.PRESET_BOTTOM_LEFT, Vector2(94, -28))
    var camera_hint := _text(self, "Rechts wischen: Kamera  ·  PC: WASD / Tab / Leertaste / E / F", Vector2.ZERO, 13, Color("d0dbce"))
    _anchor(camera_hint, Control.PRESET_BOTTOM_LEFT, Vector2(268, -30))
    attack_button = _button(self, "ANGRIFF\nAUTO", Rect2(0,0,134,106), "attack")
    _bottom_right(attack_button, Vector2(-160,-157))
    var target_button := _button(self, "ZIEL\nTab", Rect2(0,0,104,85), "target")
    _bottom_right(target_button, Vector2(-282,-124))
    interact_button = _button(self, "SPRECHEN\nE", Rect2(0,0,130,62), "interact")
    _bottom_right(interact_button, Vector2(-160,-243))
    loot_button = _button(self, "BEUTE\nF", Rect2(0,0,104,62), "loot")
    _bottom_right(loot_button, Vector2(-282,-212))
    var message_panel := _panel(Rect2(0,0,570,90))
    _anchor(message_panel, Control.PRESET_BOTTOM_LEFT, Vector2(268,-168))
    message = _text(message_panel, "", Vector2(16,12), 18, Color("f0e8d1"))
    message.size = Vector2(538, 68)
    message.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    message_panel.mouse_filter = Control.MOUSE_FILTER_IGNORE
    _connection_panel()

func _anchor(control: Control, preset: int, offset: Vector2) -> void:
    var dimensions := control.size
    control.set_anchors_and_offsets_preset(preset, Control.PRESET_MODE_KEEP_SIZE)
    control.offset_left = offset.x
    control.offset_top = offset.y
    control.offset_right = offset.x + dimensions.x
    control.offset_bottom = offset.y + dimensions.y

func _bottom_right(control: Control, pos: Vector2) -> void:
    _anchor(control, Control.PRESET_BOTTOM_RIGHT, pos)

func _panel(rect: Rect2) -> Panel:
    # Plain Panel rather than Container: explicit child layout stays stable on mobile.
    var holder := Panel.new()
    holder.mouse_filter = Control.MOUSE_FILTER_IGNORE
    holder.position = rect.position
    holder.size = rect.size
    var style := StyleBoxFlat.new()
    style.bg_color = Color(.045,.10,.13,.88)
    style.border_color = Color(.38,.56,.54,.6)
    style.set_border_width_all(1)
    style.set_corner_radius_all(9)
    holder.add_theme_stylebox_override("panel", style)
    add_child(holder)
    return holder

func _text(parent: Node, value: String, pos: Vector2, font_size: int, color := Color("edf0e3")) -> Label:
    var label := Label.new()
    label.text = value
    label.position = pos
    label.add_theme_font_size_override("font_size", font_size)
    label.add_theme_color_override("font_color", color)
    label.mouse_filter = Control.MOUSE_FILTER_IGNORE
    parent.add_child(label)
    return label

func _bar(parent: Control, rect: Rect2, color: Color) -> ProgressBar:
    var bar := ProgressBar.new()
    bar.position = rect.position
    bar.size = rect.size
    bar.show_percentage = false
    bar.mouse_filter = Control.MOUSE_FILTER_IGNORE
    var fill := StyleBoxFlat.new()
    fill.bg_color = color
    fill.set_corner_radius_all(3)
    bar.add_theme_stylebox_override("fill", fill)
    var bg := StyleBoxFlat.new()
    bg.bg_color = Color("20363f")
    bg.set_corner_radius_all(3)
    bar.add_theme_stylebox_override("background", bg)
    parent.add_child(bar)
    return bar

func _button(parent: Control, title: String, rect: Rect2, kind: String) -> Button:
    var button = TouchAction.new()
    button.text = title
    button.position = rect.position
    button.custom_minimum_size = rect.size
    button.size = rect.size
    button.focus_mode = Control.FOCUS_NONE
    button.add_theme_font_size_override("font_size", 17)
    var style := StyleBoxFlat.new()
    style.bg_color = Color(.08,.19,.23,.94)
    style.border_color = Color("6fada2")
    style.set_border_width_all(2)
    style.set_corner_radius_all(12)
    button.add_theme_stylebox_override("normal", style)
    var hover: StyleBoxFlat = style.duplicate()
    hover.bg_color = Color("28615f")
    button.add_theme_stylebox_override("hover", hover)
    var pressed_style: StyleBoxFlat = style.duplicate()
    pressed_style.bg_color = Color("467f70")
    button.add_theme_stylebox_override("pressed", pressed_style)
    parent.add_child(button)
    if kind != "online":
        button.pressed.connect(func(): action_requested.emit(kind))
        action_buttons.append(button)
    return button

func _connection_panel() -> void:
    online_panel = _panel(Rect2(360, 175, 560, 300))
    online_panel.mouse_filter = Control.MOUSE_FILTER_STOP
    online_panel.z_index = 10
    # A child Control prevents the PanelContainer from relaying out individual fields.
    var content := Control.new()
    content.size = Vector2(560,300)
    online_panel.add_child(content)
    _text(content, "GEMEINSAM DURCH NORDTOR", Vector2(24,20), 24)
    _text(content, "Online benötigt einen erreichbaren Spielserver.\nTraining und Online-Fortschritt sind getrennt und temporär.", Vector2(24,62), 16)
    endpoint = LineEdit.new()
    endpoint.position = Vector2(24,117)
    endpoint.size = Vector2(510,48)
    endpoint.placeholder_text = "wss://dein-server/ws"
    var config := ConfigFile.new()
    if config.load("user://connection.cfg") == OK:
        endpoint.text = config.get_value("network", "endpoint", "")
    content.add_child(endpoint)
    var connect_button := _button(content, "Verbinden", Rect2(24,184,245,50), "online")
    connect_button.pressed.connect(func():
        var value := endpoint.text.strip_edges()
        var saved := ConfigFile.new()
        saved.set_value("network", "endpoint", value)
        saved.save("user://connection.cfg")
        online_panel.hide()
        connect_requested.emit(value))
    var practice_button := _button(content, "Lokales Training", Rect2(288,184,246,50), "online")
    practice_button.pressed.connect(func(): online_panel.hide(); practice_requested.emit())
    var close_button := _button(content, "Schließen", Rect2(180,244,200,40), "online")
    close_button.pressed.connect(online_panel.hide)
    online_panel.hide()

func update_state(state: Dictionary, player: Dictionary, online: bool) -> void:
    if player.is_empty():
        return
    var info: Dictionary = state.self
    player_title.text = "Reisender  /  Stufe %d" % info.level
    hp.max_value = player.maxHp
    hp.value = player.hp
    hp_text.text = "%d / %d HP" % [player.hp, player.maxHp]
    xp.max_value = info.level*100
    xp.value = info.exp
    mode_label.text = "ONLINE · %d SPIELER · TEMPORÄR" % state.players.size() if online else "LOKALES TRAINING · NICHT GESPEICHERT"
    var progress: String = info.quest
    match progress:
        "available": objective.text = "Sprich mit Mira bei der Stadtwache. [E]"
        "active": objective.text = "Defekte Drohnen: %d / 3\nNordhof hinter dem Tor" % info.kills
        "ready": objective.text = "3 / 3 erledigt. Zurück zu Mira! [E]"
        "complete": objective.text = "Abgeschlossen · +60 EXP / +25 Münzen"
    inventory.text = "%d Münzen · %d Schrott\n%d / %d EXP · %d Statpunkt(e)" % [info.coins,info.scrap,info.exp,info.level*100,info.statPoints]
    if info.message != _last_message:
        _last_message = info.message
        _message_time = 9.0
        message.text = info.message
    attack_button.text = "STOPP\nAUTO" if player.attacking else "ANGRIFF\nAUTO"
    var selected := false
    for enemy in state.monsters:
        if enemy.id == player.target and not enemy.dead:
            var dist := Vector2(enemy.x-player.x,enemy.z-player.z).length()
            target.text = "%s\n%d / %d HP  ·  %.1f m" % [enemy.name,enemy.hp,enemy.maxHp,dist]
            selected = true
    if not selected:
        target.text = "Ziel wählen: Tab / ZIEL" if player.z < 0 else ""
    if player.dead:
        message.text = "BESIEGT · Wiederbelebung an der Stadtwache …"

func _process(delta: float) -> void:
    _message_time -= delta
    message.modulate.a = 1.0 if _message_time > 0 else .5
