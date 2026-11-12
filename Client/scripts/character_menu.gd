extends Panel

signal intent(kind: String,target: String)
const TouchAction = preload("res://scripts/touch_action.gd")
var catalog: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://data/game.json"))
var info: Dictionary = {}
var content: VBoxContainer
var title: Label
var tab := "Charakter"
var cooldown := 0.0

func _ready() -> void:
    position = Vector2(260,110)
    size = Vector2(710,450)
    z_index = 15
    mouse_filter = Control.MOUSE_FILTER_STOP
    var style := StyleBoxFlat.new()
    style.bg_color = Color("102b36")
    style.set_corner_radius_all(12)
    add_theme_stylebox_override("panel",style)
    var tabs := HBoxContainer.new()
    tabs.position = Vector2(12,12)
    add_child(tabs)
    for name in ["Charakter","Inventar","Quests","Pet","Layout","Schließen"]:
        var button = TouchAction.new()
        button.text = name
        button.custom_minimum_size = Vector2(106,40)
        button.pressed.connect(func():
            if name=="Schließen":hide()
            else:tab=name;_rebuild())
        tabs.add_child(button)
    var scroll := ScrollContainer.new()
    scroll.position = Vector2(16,70)
    scroll.size = Vector2(678,363)
    add_child(scroll)
    content = VBoxContainer.new()
    content.custom_minimum_size.x = 650
    scroll.add_child(content)
    hide()

func _text(value: String) -> void:
    var label := Label.new()
    label.text = value
    label.add_theme_font_size_override("font_size",17)
    content.add_child(label)

func _action(title_value: String,kind: String,target_value: String) -> void:
    var button = TouchAction.new()
    button.text = title_value
    button.custom_minimum_size.y = 42
    button.pressed.connect(func():intent.emit(kind,target_value))
    content.add_child(button)

func update_info(state: Dictionary) -> void:
    info = state
    if visible and cooldown<=0 and tab!="Layout":
        cooldown = 1.0
        _rebuild()

func _process(delta: float) -> void:
    cooldown -= delta

func _rebuild() -> void:
    for child in content.get_children():
        content.remove_child(child)
        child.queue_free()
    if info.is_empty():return
    match tab:
        "Charakter":
            _text("%s · %s · Angriff %d · Schutz %d\nMana %d / %d"%[info.name,info.classId,info.attack,info.defense,info.mana,info.maxMana])
            for key in catalog.attributes:_action("%s: %d  [+1 Statpunkt]"%[key,info.attributes.get(key,0)],"stat",key)
            _text("Farm-EXP %s / %s (Kosten wachsen exponentiell)"%[info.farmXp,info.farmCost])
            _action("Zusätzlichen Statpunkt verdienen","farm_stat","")
            _text("Ausrüstung: "+str(info.equipment))
        "Inventar":
            _text("24 konfigurierbare Stack-Slots. Händler: rechts der Wache; Werkbank: links.")
            for item in info.inventory:
                _text("%s × %d · %s"%[catalog.items[item].name,info.inventory[item],catalog.items[item].rarity])
                if catalog.items[item].get("slot","")!="":_action("Ausrüsten · "+catalog.items[item].name,"equip",item)
                if item=="potion":_action("Heiltrank benutzen","use",item)
                _action("1 verkaufen (beim Händler)","sell",item)
            for item in ["potion","iron_sword","guard_coat"]:_action("Kaufen: %s · %d Münzen"%[catalog.items[item].name,catalog.items[item].price],"buy",item)
            for recipe in catalog.recipes:_action("Werkbank: "+catalog.recipes[recipe].name,"craft",recipe)
            for node in catalog.nodes:_action("Sammeln: %s · bei (%.0f, %.0f)"%[node.name,node.x,node.z],"gather",node.id)
            _text("Berufspraxis: "+str(info.professions))
        "Quests":
            _text("Miras Hauptauftrag bleibt rechts im HUD. Zusatzaufträge laufen automatisch bei passenden Tätigkeiten.")
            for q in info.quests:
                _text("%s [%s] · %d / %d · %s"%[q.name,q.type,q.progress,q.required,"abgegeben" if q.claimed else ("offen" if q.unlocked else "Voraussetzung fehlt")])
                if not q.claimed:_action("Beim Händler abgeben","claim",q.id)
        "Pet":
            _text("Arkaner Funke · Stufe %d · %d EXP · Modus: %s\nEin aktiver Begleiter. Zucht/Evolution: Datenmodell, noch nicht spielbar."%[info.pet.level,info.pet.exp,info.pet.mode])
            for mode in ["combat","heal","loot","off"]:_action("Begleiter: "+mode,"pet",mode)

        "Layout":
            _text("Touchpositionen in 12-Pixel-Schritten ändern. Lokal gespeichert.")
            var choose := OptionButton.new()
            for key in ["attack","target","interact","loot","joystick","skill1","skill2","skill3","skill4","skill5","skill6"]:choose.add_item(key)
            choose.custom_minimum_size.y = 42
            content.add_child(choose)
            for direction in ["left","right","up","down"]:
                var button = TouchAction.new()
                button.text = direction
                button.custom_minimum_size.y = 42
                button.pressed.connect(func():intent.emit("layout",choose.get_item_text(choose.selected)+"|"+direction))
                content.add_child(button)
            _action("Standardpositionen wiederherstellen","layout","reset")
