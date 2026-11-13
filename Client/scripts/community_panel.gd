extends Panel
signal intent(kind: String,target: String,text: String)
const TouchAction=preload("res://scripts/touch_action.gd")
var catalog: Dictionary=JSON.parse_string(FileAccess.get_file_as_string("res://data/game.json"))
var state: Dictionary={}
var online:=false
var tab:="Wohnen"
var content: VBoxContainer
var homes: OptionButton
var peers: OptionButton
var items: OptionButton
var names: LineEdit
var details: Label
var refresh_button: Button
var x_pos: SpinBox
var z_pos: SpinBox
var rotation_picker: SpinBox
var amount: SpinBox
var price: SpinBox
func _ready() -> void:
    position=Vector2(170,90);size=Vector2(940,520);z_index=20;mouse_filter=Control.MOUSE_FILTER_STOP
    var style:=StyleBoxFlat.new();style.bg_color=Color("102b36");style.set_corner_radius_all(12);add_theme_stylebox_override("panel",style)
    var tabs:=HBoxContainer.new();tabs.position=Vector2(15,12);add_child(tabs)
    for name in ["Wohnen","Gilde","Auktion","Arena","Schließen"]:
        var button=TouchAction.new();button.text=name;button.custom_minimum_size=Vector2(177,42);tabs.add_child(button)
        button.pressed.connect(func():if name=="Schließen":hide()
        else:tab=name;rebuild())
    details=Label.new();details.position=Vector2(18,62);details.size=Vector2(900,38);details.add_theme_font_size_override("font_size",17);add_child(details)
    var scroll:=ScrollContainer.new();scroll.position=Vector2(18,105);scroll.size=Vector2(904,395);add_child(scroll)
    content=VBoxContainer.new();content.custom_minimum_size.x=875;scroll.add_child(content)
    hide()
func update_state(value: Dictionary,is_online: bool) -> void:
    state=value;online=is_online
    if visible:details.text=str(state.get("self",{}).get("message","")) if online else "Dauerhafte Weltfunktionen benötigen einen eigenen Online-Charakter."
func text(value: String) -> void:
    var label:=Label.new();label.text=value;label.autowrap_mode=TextServer.AUTOWRAP_WORD_SMART;label.custom_minimum_size.x=860;content.add_child(label)
func button(label: String,callback: Callable) -> Button:
    var action=TouchAction.new();action.text=label;action.custom_minimum_size.y=44;action.pressed.connect(callback);content.add_child(action);return action
func emit_action(kind: String,target: String="",value: String="") -> void:
    if online:intent.emit(kind,target,value)
func option() -> OptionButton:
    var choose:=OptionButton.new();choose.custom_minimum_size.y=44;content.add_child(choose);return choose
func spin(minimum: float,maximum: float,value: float,step: float=1) -> SpinBox:
    var field:=SpinBox.new();field.min_value=minimum;field.max_value=maximum;field.value=value;field.step=step;field.custom_minimum_size.y=44;content.add_child(field);return field
func input(placeholder: String) -> LineEdit:
    var field:=LineEdit.new();field.placeholder_text=placeholder;field.max_length=64;field.custom_minimum_size.y=44;content.add_child(field);return field
func selected(choose: OptionButton) -> String:return str(choose.get_item_metadata(choose.selected)) if choose and choose.item_count>0 else ""
func peer_picker() -> OptionButton:
    var choose:=option()
    for peer in state.get("social",{}).get("online",[]):choose.add_item(peer.name+" · "+peer.id.left(6));choose.set_item_metadata(choose.item_count-1,peer.id)
    return choose
func item_picker() -> OptionButton:
    var choose:=option()
    for item in state.get("self",{}).get("character",{}).get("inventory",{}):choose.add_item(catalog.items[item].name+" · "+item);choose.set_item_metadata(choose.item_count-1,item)
    return choose
func rebuild() -> void:
    for child in content.get_children():content.remove_child(child);child.queue_free()
    update_state(state,online)
    if not online:text("Online anmelden. Hier werden keine persistenten Trainings-Ersatzwelten angelegt.");return
    var community: Dictionary=state.get("community",{})
    refresh_button=button("Anzeige aktualisieren (Eingabewerte werden zurückgesetzt)",rebuild)
    match tab:
        "Wohnen":
            text("Elin/Wohnamt (30,24): 3 Holz + selbst gebauter Eichenstuhl → freie Wohnung. Eingang links an der Südseite. Eigentümer ID und Räume:")
            homes=option()
            for home in community.get("homes",[]):
                if not home.guildHall:homes.add_item("%s · %s · %d Zimmer"%[home.name,"frei" if home.owner=="" else home.owner.left(6),home.rooms]);homes.set_item_metadata(homes.item_count-1,home.id)
            button("Einführungsquest bei Elin annehmen",func():emit_action("home_intro"))
            button("Ausgewählte freie Wohnung erhalten",func():emit_action("home_claim",selected(homes)))
            button("Miete: 10 Münzen je Woche / Rückstand begleichen",func():emit_action("home_rent",selected(homes)))
            button("Angrenzendes Zimmer: 8 Holz + 20 Münzen",func():emit_action("home_expand",selected(homes)))
            text("Möbel: Weltkoordinaten, Drehung in Grad. Im eigenen Raum stehen; Tür freihalten.")
            var player: Dictionary={}
            var own_id: String=state.get("self",{}).get("character",{}).get("id","")
            for p in state.get("players",[]):
                if p.id==own_id:player=p
            x_pos=spin(-89,89,player.get("x",0),.25);x_pos.prefix="X: "
            z_pos=spin(-89,89,player.get("z",0)-2,.25);z_pos.prefix="Z: "
            rotation_picker=spin(-180,180,0,15);rotation_picker.suffix="°"
            button("Eichenstuhl platzieren",func():emit_action("home_place",selected(homes),"oak_chair|%.2f|%.2f|%.3f"%[x_pos.value,z_pos.value,deg_to_rad(rotation_picker.value)]))
            for home in community.get("homes",[]):
                if home.owner!=own_id:continue
                text("Deine Wohnung: %s · Mietende UTC %d · Lager %s"%[home.name,home.rentUntil,str(home.get("storage",{}))])
                if home.get("furniture") is Array:
                    for object in home.furniture:
                        var furniture_id: String=object.id;var house_id: String=home.id
                        button("Möbel %s an Eingabeposition verschieben/drehen"%furniture_id.left(6),func():emit_action("home_move",house_id,"%s|%.2f|%.2f|%.3f"%[furniture_id,x_pos.value,z_pos.value,deg_to_rad(rotation_picker.value)]))
                        button("Möbel %s ins Inventar zurück"%furniture_id.left(6),func():emit_action("home_remove",house_id,furniture_id))
            peers=peer_picker()
            button("Auswahl als Besucher erlauben",func():emit_action("home_visit",selected(homes),selected(peers)+"|allow"))
            button("Besuchsrecht der Auswahl widerrufen",func():emit_action("home_visit",selected(homes),selected(peers)+"|remove"))
            names=input("Offline-Besucher: vollständige Charakter-ID")
            button("ID-Besucher erlauben",func():emit_action("home_visit",selected(homes),names.text+"|allow"))
            button("ID-Besucher entfernen",func():emit_action("home_visit",selected(homes),names.text+"|remove"))
            button("Gildenbesuche erlauben",func():emit_action("home_visit",selected(homes),"guild|allow"))
            button("Gildenbesuche sperren",func():emit_action("home_visit",selected(homes),"guild|remove"))
            items=item_picker();amount=spin(1,99,1)
            button("Ausgewähltes Item ins Wohnungslager",func():emit_action("home_put",selected(homes),selected(items)+"|%d"%amount.value))
            for home in community.get("homes",[]):
                if home.owner==own_id and home.get("storage") is Dictionary:
                    for item in home.storage:
                        var item_id: String=item;var house_id: String=home.id
                        button("1× %s aus Wohnungslager"%catalog.items[item].name,func():emit_action("home_take",house_id,item_id+"|1"))
            for recipe in catalog.recipes:
                var recipe_id: String=recipe
                button("Wohnungswerkstatt: "+catalog.recipes[recipe].name,func():emit_action("home_craft",selected(homes),recipe_id))
            var pets: Array=state.get("self",{}).get("character",{}).get("ownedPets",[])
            for pet in pets:
                var pet_id: String=pet.id
                button("Zucht mit Pet "+pet_id.left(8),func():emit_action("home_breed",selected(homes),pet_id))
            button("Garten: 1 Kraut pflanzen (5 Minuten)",func():emit_action("home_plant",selected(homes)))
            button("Garten: reife Ernte abholen",func():emit_action("home_harvest",selected(homes)))
        "Gilde":
            text("Register bei Elin (30,24). Gildenlager hier oder im eigenen physischen Gildenhaus.")
            names=input("Gildenname: 3–24 Buchstaben/Ziffern")
            button("Gilde gründen: 20 Münzen",func():emit_action("guild_create","",names.text))
            for invite in community.get("guildInvites",[]):
                var guild_id: String=invite.id
                button("Einladung annehmen: "+invite.name,func():emit_action("guild_accept",guild_id))
            var guild=community.get("guild")
            if guild is Dictionary:
                text("%s · St. %d · %d EXP · Lager %s"%[guild.name,guild.level,guild.exp,str(guild.storage)])
                for member in guild.members:text(member+" · "+str(guild.members[member]))
                peers=peer_picker()
                for config in [["Einladen","guild_invite",""],["Entfernen","guild_kick",""],["Offizier","guild_rank","officer"],["Mitglied","guild_rank","member"],["Leitung übertragen","guild_rank","leader"]]:
                    var kind: String=config[1];var value: String=config[2]
                    button(config[0]+": ausgewählter Spieler",func():emit_action(kind,selected(peers),value))
                button("Gildenhaus freigeben (nur Leitung, Lager bleibt)",func():emit_action("guild_release"))
                button("Gilde verlassen",func():emit_action("guild_leave"))
                button("Versorgungsauftrag: 3 Holz + 2 Erz → 25 Gilden-EXP / 5 Münzen",func():emit_action("guild_quest"))
                button("Gildensegen aktivieren: 25 Gildenenergie",func():emit_action("guild_buff"))
                button("Gildenhaus erwerben: 20 Holz + 10 Erz im Lager",func():emit_action("guild_hall","guildhall-1"))
                items=item_picker();amount=spin(1,99,1)
                button("Auswahl ins Gildenlager",func():emit_action("guild_put","",selected(items)+"|%d"%amount.value))
                for item in guild.storage:
                    var item_id: String=item
                    button("1× "+catalog.items[item].name+" aus Lager (Offizier/Leitung)",func():emit_action("guild_take","",item_id+"|1"))
        "Auktion":
            text("Börse bei (30,12). Gesamtpreise; 5 % Verkaufsgebühr (mindestens 1 Münze). Items bleiben bis Kauf/Rücknahme im Escrow.")
            names=input("Suche nach Itemname")
            button("Suche senden, danach Anzeige aktualisieren",func():emit_action("market_search","",names.text))
            items=item_picker();amount=spin(1,99,1);amount.prefix="Menge: "
            price=spin(1,100000,10);price.prefix="Gesamtpreis: "
            button("Verbindlich einstellen",func():emit_action("market_list","",selected(items)+"|%d|%d"%[amount.value,price.value]))
            text("Offene Erlöse: %d Münzen"%community.get("proceeds",0))
            button("Verkaufserlöse abholen",func():emit_action("market_claim"))
            text("Seite %d / %d"%[community.get("marketPage",0)+1,community.get("marketPages",1)])
            button("Vorherige Seite, danach aktualisieren",func():emit_action("market_page",str(maxi(0,int(community.get("marketPage",0))-1))))
            button("Nächste Seite, danach aktualisieren",func():emit_action("market_page",str(int(community.get("marketPage",0))+1)))
            for offer in community.get("auctions",[]):
                var listing_id: String=offer.id
                button("Kaufen: %s × %d · %d Münzen"%[catalog.items[offer.item].name,offer.count,offer.price],func():emit_action("market_buy",listing_id))
            for offer in community.get("ownAuctions",[]):
                var listing_id: String=offer.id
                button("Eigenes Angebot zurücknehmen: %s × %d"%[catalog.items[offer.item].name,offer.count],func():emit_action("market_cancel",listing_id))
        "Arena":
            button("Tiefwerk-Transit benutzen: bei (0,-40), St. 5 + Miras Auftrag",func():emit_action("transit"))
            text("PvP ist ausschließlich in der markierten Arena möglich. Normierte Match-HP und Angriffe sind vom PvE-Fortschritt getrennt.")
            peers=peer_picker()
            button("Team-Bereitschaft umschalten (alle Mitglieder)",func():emit_action("arena_ready"))
            button("Gruppenleiter zu Teamkampf fordern",func():emit_action("arena_team_invite",selected(peers)))
            button("Gilden-Teamkampf in Arena fordern",func():emit_action("arena_war_invite",selected(peers)))
            button("Auswahl zum Arenaduell fordern",func():emit_action("arena_invite",selected(peers)))
            button("Duellanfrage der Auswahl annehmen",func():emit_action("arena_accept",selected(peers)))
            button("Auswahl angreifen (nur aktives Match / Reichweite)",func():emit_action("arena_hit",selected(peers)))
            button("Match aufgeben",func():emit_action("arena_leave"))
            text(str(state.get("arena",{})))
