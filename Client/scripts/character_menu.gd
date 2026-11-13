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
    for name in ["Charakter","Aussehen","Skills","Inventar","Quests","Pet","Automatik","Layout","Schließen"]:
        var button = TouchAction.new()
        button.text = name
        button.add_theme_font_size_override("font_size",16)
        button.custom_minimum_size = Vector2(73,40)
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
    if visible and cooldown<=0 and tab not in ["Layout","Skills","Aussehen"]:
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
            _text("Prestige: "+str(info.get("prestige",0)))
            _action("Stat-Reset: Stufe 10 + seltener Resetstein","reset_stats","")
            _action("Prestige: Stufe 100 + Bosskern (beim Händler)","prestige","")
            _text("Ausrüstung: "+str(info.equipment))
        "Aussehen":
            _text("Online-Erscheinung: Geschlecht, Gesicht, Körper, Frisur, Haut und Tattoo.")
            var choices: Array=[]
            for options in [["male","female","neutral"],["Gesicht 1","Gesicht 2","Gesicht 3","Gesicht 4"],["Körper 1","Körper 2","Körper 3","Körper 4"],["Frisur 1","Frisur 2","Frisur 3","Frisur 4"],["Haut 1","Haut 2","Haut 3","Haut 4"],["Kein Tattoo","Tattoo 1","Tattoo 2","Tattoo 3"]]:
                var choose:=OptionButton.new();choose.custom_minimum_size.y=42
                for value in options:choose.add_item(value)
                content.add_child(choose);choices.append(choose)
            var save=TouchAction.new();save.text="Erscheinung online speichern";save.custom_minimum_size.y=44
            save.pressed.connect(func():intent.emit("appearance","%s|%d|%d|%d|%d|%d"%[choices[0].get_item_text(choices[0].selected),choices[1].selected,choices[2].selected,choices[3].selected,choices[4].selected,choices[5].selected]))
            content.add_child(save)
        "Skills":
            _text("Acht aktive Skills. Level schaltet frei; sechs Plätze sind gleichzeitig sichtbar.")
            var slot_picker:=OptionButton.new()
            for i in range(6):slot_picker.add_item("Zielplatz %d"%(i+1))
            slot_picker.custom_minimum_size.y=42
            content.add_child(slot_picker)
            for skill in info.get("skillbook",info.skills):
                _text("%s · Stufe %d · %d Mana"%[skill.name,skill.get("minLevel",1),skill.cost])
                var binding_button=TouchAction.new()
                binding_button.text="Auf gewählten Platz legen"
                binding_button.custom_minimum_size.y=42
                binding_button.pressed.connect(func():intent.emit("slot","%d|%s"%[slot_picker.selected,skill.id]))
                content.add_child(binding_button)
        "Inventar":
            _text("24 konfigurierbare Stack-Slots. Händler: rechts der Wache; Werkbank: links.")
            for id in info.get("gear",{}):
                var instance: Dictionary=info.gear[id]
                _text("%s · Zustand %d / 100 · %s"%[catalog.items[instance.item].name,instance.condition,id.left(6)])
                _action("Instanz ausrüsten","gear_equip",id)
                _action("Beim Händler reparieren (1 Münze je Zustandspunkt)","gear_repair",id)
            for slot in info.get("equippedGear",{}):_action("Instanz ablegen: "+slot,"gear_remove",slot)
            for slot in info.equipment:_action("Alte Ausrüstung ablegen: "+slot,"gear_legacy_remove",slot)
            for item in info.inventory:
                _text("%s × %d · %s"%[catalog.items[item].name,info.inventory[item],catalog.items[item].rarity])
                if catalog.items[item].get("slot","")!="" and info.id!="local":_action("In eindeutige Instanz umwandeln (vorher ablegen)","gear_unpack",item)
                if catalog.items[item].get("slot","")!="":_action("Ausrüsten · "+catalog.items[item].name,"equip",item)
                if item=="potion":_action("Heiltrank benutzen","use",item)
                _action("1 verkaufen (beim Händler)","sell",item)
            for item in ["potion","iron_sword","guard_coat"]:_action("Kaufen: %s · %d Münzen"%[catalog.items[item].name,catalog.items[item].price],"buy",item)
            for recipe in catalog.recipes:_action("Werkbank: "+catalog.recipes[recipe].name,"craft",recipe)
            for node in catalog.nodes:_action("Sammeln: %s · bei (%.0f, %.0f)"%[node.name,node.x,node.z],"gather",node.id)
            for profession in catalog.professions:
                var experience: int=info.professions.get(profession,0)
                _text("%s · St. %d · %d EXP"%[profession,1+experience/int(catalog.professionLevelXp),experience])
        "Quests":
            _text("Miras Hauptauftrag bleibt rechts im HUD. Zusatzaufträge laufen automatisch bei passenden Tätigkeiten.")
            for q in info.quests:
                _text("%s [%s] · %d / %d · %s"%[q.name,q.type,q.progress,q.required,"abgegeben" if q.claimed else ("offen" if q.unlocked else "Voraussetzung fehlt")])
                if not q.claimed:_action("Beim Händler abgeben","claim",q.id)
        "Pet":
            _text("Arkaner Funke · Stufe %d · %d EXP · Modus: %s\nEin aktiver Begleiter · Entwicklung und Zucht an der Werkbank."%[info.pet.level,info.pet.exp,info.pet.mode])
            for mode in ["combat","heal","tank","buff","loot","gather","off"]:_action("Begleiter: "+mode,"pet",mode)

            _text("Eigenschaften: "+str(info.pet.get("traits",{}))+" · Entwicklung "+str(info.pet.evolution))
            _text("Pet-Tasche: "+str(info.pet.get("inventory",{})))
            _action("Pet-Tasche übernehmen","pet_take","")
            _action("Begleitereischale ausbrüten (Werkbank)","pet_hatch","")
            _action("Entwickeln: St. 5 + Evolutionssplitter","pet_evolve","")
            _action("Offline trainieren: inaktiv + Futter, 5 Minuten","pet_train","")
            if info.pet.get("trainingUntil",0)>0:_text("Training: noch %.0f Sekunden"%maxf(0,info.pet.trainingUntil-Time.get_unix_time_from_system()))
            for companion in info.get("ownedPets",[]):
                _text("%s · St. %d · Eltern %d"%[companion.id.left(8),companion.level,companion.parents.size()])
                _action("Diesen Begleiter auswählen","pet_select",companion.id)
                if companion.id!=info.pet.id:_action("Mit aktiv gewähltem Pet kreuzen (2× Futter)","pet_breed",companion.id)
        "Automatik":
            _text("Bewachte Automatik am sicheren Stadtrand · maximal 60 Sekunden / Radius 14.")
            _text("Manuelle Bewegung beendet sofort. HP-/Mana-/Inventargrenzen lösen Rückkehr aus.")
            _text("Kein Offline-Farmen. Ein blockierter Rückweg wird abgebrochen, nicht teleportiert.")
            var state: Dictionary=info.get("automation",{})
            _text("Aktiv: %s · Rückkehr: %s · %.0f Sekunden"%[state.get("active",false),state.get("returning",false),state.get("remaining",0)])
            _action("Automatik starten · Radius 14","farm_start","14")
            _action("Automatik sofort stoppen","farm_stop","")
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
