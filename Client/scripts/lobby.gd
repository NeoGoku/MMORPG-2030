extends Panel

signal play_requested(url: String, ticket: String)
signal local_requested(slot: int, nickname: String, class_id: String)
const TouchAction = preload("res://scripts/touch_action.gd")
var catalog: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://data/game.json"))
var base_url: LineEdit
var username: LineEdit
var password: LineEdit
var nickname: LineEdit
var classes: OptionButton
var slots: OptionButton
var status: Label
var http := HTTPRequest.new()
var token := ""
var characters: Array = []
const AppearanceCreator=preload("res://scripts/appearance_creator.gd")
var creator=AppearanceCreator.new()
var creation_appearance:="neutral|0|0|0|0|0"
var busy := false

func _ready() -> void:
    size = Vector2(880,540)
    position = Vector2(200,90)
    z_index = 20
    mouse_filter = Control.MOUSE_FILTER_STOP
    var background := StyleBoxFlat.new()
    background.bg_color = Color("102b36")
    background.set_corner_radius_all(12)
    add_theme_stylebox_override("panel",background)
    add_child(creator)
    creator.position=Vector2(-50,-35)
    creator.chosen.connect(func(value: String):creation_appearance=value)
    add_child(http)
    http.timeout = 10
    _label("CHARAKTERE · 3 PLÄTZE / 8 KLASSEN",Vector2(24,18),24)
    base_url = _field("https://dein-server",Vector2(24,64),Vector2(830,42))
    var saved := ConfigFile.new()
    if saved.load("user://connection.cfg")==OK:
        var old: String = saved.get_value("network","endpoint","")
        base_url.text = old.replace("wss://","https://").replace("ws://","http://").trim_suffix("/ws")
    username = _field("Account (a-z, 0-9, _)",Vector2(24,120),Vector2(400,42))
    password = _field("Passwort, mindestens 12 Zeichen",Vector2(448,120),Vector2(405,42))
    password.secret = true
    _button("Registrieren",Rect2(24,176,260,44),func(): _login(true))
    _button("Anmelden / Laden",Rect2(304,176,270,44),func(): _login(false))
    _button("Schließen",Rect2(594,176,260,44),hide)
    slots = OptionButton.new()
    slots.position = Vector2(24,240)
    slots.size = Vector2(830,42)
    for i in range(3):slots.add_item("Platz %d"%(i+1))
    add_child(slots)
    nickname = _field("Charaktername (3–20 Zeichen)",Vector2(24,300),Vector2(400,42))
    classes = OptionButton.new()
    classes.position = Vector2(448,300)
    classes.size = Vector2(405,42)
    for key in catalog.classes: classes.add_item(key)
    add_child(classes)
    _button("Online: Erstellen",Rect2(24,360,260,50),_create)
    _button("Online: Spielen",Rect2(304,360,270,50),_play)
    _button("Lokales Training",Rect2(594,360,260,50),func():
        hide()
        local_requested.emit(slots.selected,nickname.text if nickname.text.length()>=3 else "Reisender",classes.get_item_text(classes.selected)))
    _button("Aussehen / 3D-Vorschau",Rect2(24,418,400,38),func():creator.open_for_class(classes.get_item_text(classes.selected)))
    status = _label("Online speichert auf dem Server. Lokal bleibt getrennt und temporär.\nKein öffentlicher Spielserver eingerichtet. Passwörter werden nicht gespeichert.",Vector2(24,466),16)
    status.size = Vector2(830,65)
    status.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
    hide()

func _label(value: String,pos: Vector2,font_size: int) -> Label:
    var label := Label.new()
    label.text = value
    label.position = pos
    label.add_theme_font_size_override("font_size",font_size)
    add_child(label)
    return label

func _field(hint: String,pos: Vector2,dimensions: Vector2) -> LineEdit:
    var field := LineEdit.new()
    field.placeholder_text = hint
    field.position = pos
    field.size = dimensions
    add_child(field)
    return field

func _button(title: String,rect: Rect2,callback: Callable) -> void:
    var button = TouchAction.new()
    button.text = title
    button.position = rect.position
    button.size = rect.size
    button.focus_mode = Control.FOCUS_NONE
    button.pressed.connect(callback)
    add_child(button)

func _request(path: String, body: Dictionary, method: int) -> Dictionary:
    var root := base_url.text.strip_edges().trim_suffix("/")
    if not root.begins_with("https://") and not root.begins_with("http://127.0.0.1:") and not root.begins_with("http://localhost:"):
        return {"error":"Online-Anmeldung benötigt HTTPS; HTTP ist nur auf Loopback für Entwicklung erlaubt."}
    var headers := PackedStringArray(["Content-Type: application/json"])
    if not token.is_empty():headers.append("Authorization: Bearer "+token)
    var error := http.request(root+path,headers,method,JSON.stringify(body) if method!=HTTPClient.METHOD_GET else "")
    if error != OK:return {"error":"Anfrage konnte nicht starten."}
    var result = await http.request_completed
    if result[0]!=HTTPRequest.RESULT_SUCCESS:return {"error":"Server nicht erreichbar oder TLS/CORS-Konfiguration fehlt."}
    var parsed = JSON.parse_string(result[3].get_string_from_utf8())
    if result[1]<200 or result[1]>=300:return {"error":parsed.get("error","Anmeldung/Aktion abgewiesen (%d)."%result[1]) if parsed is Dictionary else "Anfrage abgewiesen."}
    return {"data":parsed}

func _login(register: bool) -> void:
    if busy:return
    busy = true
    token = ""
    status.text = "Anmeldung …"
    var response: Dictionary = await _request("/api/register" if register else "/api/login",{"username":username.text,"password":password.text},HTTPClient.METHOD_POST)
    password.text = ""
    if response.has("error"):status.text=response.error
    else:
        token = response.data.token
        var list: Dictionary = await _request("/api/characters",{},HTTPClient.METHOD_GET)
        if list.has("error"):status.text=list.error
        else:
            characters = list.data
            _show_slots()
            status.text = "Angemeldet. PostgreSQL dauerhaft." if response.data.persistent else "Angemeldet: flüchtiger Server-Testbetrieb, KEINE dauerhafte Speicherung!"
    busy = false

func _show_slots() -> void:
    for i in range(3):
        var title := "Platz %d · leer"%(i+1)
        for c in characters:
            if c.slot==i:title="Platz %d · %s · %s · Stufe %d"%[i+1,c.name,c.classId,c.state.level]
        slots.set_item_text(i,title)

func _create() -> void:
    if busy or token.is_empty():
        status.text = "Zuerst anmelden."
        return
    busy = true
    var response: Dictionary = await _request("/api/characters",{"slot":slots.selected,"name":nickname.text,"classId":classes.get_item_text(classes.selected),"appearance":creation_appearance},HTTPClient.METHOD_POST)
    if response.has("error"):status.text=response.error
    else:
        characters.append(response.data)
        _show_slots()
        status.text = "Charakter erstellt. Online: Spielen wählen."
    busy = false

func _play() -> void:
    if busy or token.is_empty():return
    var id := ""
    for c in characters:
        if c.slot==slots.selected:id=c.id
    if id.is_empty():
        status.text = "Diesen Platz zuerst erstellen."
        return
    busy = true
    var response: Dictionary = await _request("/api/ticket",{"characterId":id},HTTPClient.METHOD_POST)
    busy = false
    if response.has("error"):status.text=response.error
    else:
        var root := base_url.text.strip_edges().trim_suffix("/")
        var saved := ConfigFile.new()
        saved.set_value("network","endpoint",root)
        saved.save("user://connection.cfg")
        hide()
        play_requested.emit(root.replace("https://","wss://").replace("http://","ws://")+"/ws",response.data.ticket)
