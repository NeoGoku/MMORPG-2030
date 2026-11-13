extends Panel

signal intent(kind: String,target: String,text: String)
const TouchAction = preload("res://scripts/touch_action.gd")
var peers: OptionButton
var group_box: VBoxContainer
var history: TextEdit
var channel: OptionButton
var message: LineEdit
var trade_box: Control
var offer_item: OptionButton
var offer_count: SpinBox
var offer_coins: SpinBox
var offer_label: Label
var tab := "Gruppe"
var group_scroll: ScrollContainer
var last_state: Dictionary = {}
var refresh := 0.0
var online := false
var local_notice: Label
var catalogue: Dictionary = JSON.parse_string(FileAccess.get_file_as_string("res://data/game.json"))

func _ready() -> void:
    position=Vector2(190,100);size=Vector2(900,500);z_index=18
    mouse_filter=Control.MOUSE_FILTER_STOP
    var style:=StyleBoxFlat.new();style.bg_color=Color("102b36");style.set_corner_radius_all(12);add_theme_stylebox_override("panel",style)
    var tabs:=HBoxContainer.new();tabs.position=Vector2(18,14);add_child(tabs)
    for name in ["Gruppe","Chat","Handel","Schließen"]:
        var button=TouchAction.new();button.text=name;button.custom_minimum_size=Vector2(210,40)
        button.pressed.connect(func():
            if name=="Schließen":hide()
            else:tab=name;_show_tab())
        tabs.add_child(button)
    peers=OptionButton.new();peers.position=Vector2(18,67);peers.size=Vector2(860,42);add_child(peers)
    local_notice=_label("Echte Gruppen, Chat und Spielerhandel benötigen eine Online-Verbindung.",Vector2(20,117),18)
    group_scroll=ScrollContainer.new();group_scroll.position=Vector2(18,150);group_scroll.size=Vector2(860,330);add_child(group_scroll)
    group_box=VBoxContainer.new();group_box.custom_minimum_size.x=830;group_scroll.add_child(group_box)
    history=TextEdit.new();history.editable=false;history.position=Vector2(18,150);history.size=Vector2(860,225);history.add_theme_font_size_override("font_size",16);add_child(history)
    channel=OptionButton.new();channel.position=Vector2(18,390);channel.size=Vector2(180,42)
    for label in ["Lokal","Gruppe","Privat","Gilde"]:channel.add_item(label)
    add_child(channel)
    message=LineEdit.new();message.position=Vector2(210,390);message.size=Vector2(520,42);message.max_length=240;message.placeholder_text="Nachricht …";add_child(message)
    var send=_button("Senden",Rect2(744,390,134,42),func():
        if online and not message.text.is_empty():
            intent.emit(["chat_local","chat_party","chat_private","chat_guild"][channel.selected],_peer(),message.text)
            message.text="")
    send.name="SendChat"
    trade_box=Control.new();trade_box.position=Vector2(18,150);trade_box.size=Vector2(860,330);add_child(trade_box)
    offer_label=Label.new();offer_label.position=Vector2(0,0);offer_label.size=Vector2(850,100);offer_label.autowrap_mode=TextServer.AUTOWRAP_WORD_SMART;trade_box.add_child(offer_label)
    offer_item=OptionButton.new();offer_item.position=Vector2(0,110);offer_item.size=Vector2(350,42);trade_box.add_child(offer_item)
    offer_count=SpinBox.new();offer_count.position=Vector2(365,110);offer_count.size=Vector2(160,42);offer_count.min_value=0;offer_count.max_value=99;trade_box.add_child(offer_count)
    offer_coins=SpinBox.new();offer_coins.position=Vector2(540,110);offer_coins.size=Vector2(200,42);offer_coins.min_value=0;offer_coins.max_value=100000;offer_coins.suffix="Münzen";trade_box.add_child(offer_coins)
    _trade_button("Handel öffnen",Vector2(0,180),func():if online:intent.emit("trade_open",_peer(),""))
    _trade_button("Angebot setzen",Vector2(215,180),func():
        if online:intent.emit("trade_offer","","%s|%d|%d"%[offer_item.get_item_metadata(offer_item.selected),int(offer_count.value),int(offer_coins.value)]))
    _trade_button("Bestätigen",Vector2(430,180),func():if online:intent.emit("trade_confirm","",""))
    _trade_button("Abbrechen",Vector2(645,180),func():intent.emit("trade_cancel","",""))
    _show_tab();hide()

func _label(text: String,pos: Vector2,font_size: int) -> Label:
    var label:=Label.new();label.text=text;label.position=pos;label.add_theme_font_size_override("font_size",font_size);add_child(label);return label
func _button(text: String,rect: Rect2,callback: Callable) -> Button:
    var button=TouchAction.new();button.text=text;button.position=rect.position;button.size=rect.size;button.pressed.connect(callback);button.focus_mode=Control.FOCUS_NONE;add_child(button);return button
func _trade_button(text: String,pos: Vector2,callback: Callable) -> void:
    var button=TouchAction.new();button.text=text;button.position=pos;button.size=Vector2(205,48);button.pressed.connect(callback);trade_box.add_child(button)
func _peer() -> String:
    return str(peers.get_item_metadata(peers.selected)) if peers.item_count>0 else ""
func _group_action(text: String,kind: String,target: String) -> void:
    var button=TouchAction.new();button.text=text;button.custom_minimum_size.y=40;button.pressed.connect(func():if online:intent.emit(kind,_peer() if target=="@selected" else target,""));group_box.add_child(button)
func _group_text(text: String) -> void:
    var label:=Label.new();label.text=text;group_box.add_child(label)
func _show_tab() -> void:
    if not group_scroll:return
    group_scroll.visible=tab=="Gruppe"
    history.visible=tab=="Chat";channel.visible=tab=="Chat";message.visible=tab=="Chat";get_node("SendChat").visible=tab=="Chat"
    trade_box.visible=tab=="Handel"
    _refresh_view()
func update_state(state: Dictionary,is_online: bool) -> void:
    last_state=state;online=is_online
    if visible and refresh<=0:refresh=1;_refresh_view()
func _process(dt: float) -> void:refresh-=dt
func _refresh_view() -> void:
    if not peers:return
    local_notice.text=str(last_state.get("self",{}).get("message","Online: Gruppe / Chat / Handel")) if online else "Nur online verfügbar. Im Training werden keine Mitspieler erfunden."
    var data: Dictionary=last_state.get("social",{}) if last_state.get("social") is Dictionary else {}
    var selected:=_peer();peers.clear()
    for peer in data.get("online",data.get("nearby",[])):
        peers.add_item("%s · St. %d · %s"%[peer.name,peer.level,peer.id.left(6)]);peers.set_item_metadata(peers.item_count-1,peer.id)
        if peer.id==selected:peers.select(peers.item_count-1)
    for child in group_box.get_children():group_box.remove_child(child);child.queue_free()
    if data.get("party") is Dictionary:
        var party: Dictionary=data.party
        _group_text("Gruppe · %d / 10 · Leiter %s"%[party.members.size(),party.leader.left(6)])
        for member in party.members:
            _group_text("%s · %s · HP %d"%[member.name,"online" if member.online else "offline",member.hp])
        _group_action("Gruppe verlassen","party_leave","")
        _group_action("Ausgewählten Spieler entfernen","party_kick","@selected")
        _group_action("Leitung an Auswahl übertragen","party_promote","@selected")
    else:_group_text("Noch keine Gruppe.")
    var invite=TouchAction.new();invite.text="Ausgewählten Spieler einladen";invite.custom_minimum_size.y=44;invite.pressed.connect(func():if online:intent.emit("party_invite",_peer(),""));group_box.add_child(invite)
    for pending in data.get("invitations",[]):_group_action("Einladung von %s annehmen"%pending.name,"party_accept",pending.leader)
    var lines:=PackedStringArray()
    for entry in data.get("chat",[]):lines.append("[%s] %s: %s"%[entry.kind.trim_prefix("chat_"),entry.name,entry.text])
    var value: String="\n".join(lines)
    if history.text!=value:history.text=value;history.set_caret_line(maxi(0,history.get_line_count()-1))
    var trade=data.get("trade")
    offer_label.text="Zwei angemeldete Spieler in 5 m Reichweite. Beide bestätigen dasselbe Angebot."
    if trade is Dictionary:
        offer_label.text="Handel · %.0f s\n"%trade.remaining
        for offer in trade.offers:offer_label.text+="%s: %s × %d + %d Münzen · %s\n"%[offer.owner.left(6),offer.item,offer.count,offer.coins,"bestätigt" if offer.confirmed else "offen"]
    var selected_item: String=str(offer_item.get_item_metadata(offer_item.selected)) if offer_item.item_count>0 else ""
    offer_item.clear();offer_item.add_item("Kein Gegenstand");offer_item.set_item_metadata(0,"")
    var character: Dictionary=last_state.get("self",{}).get("character",{})
    for item in character.get("inventory",{}):
        offer_item.add_item("%s × %d"%[catalogue.items[item].name,character.inventory[item]]);offer_item.set_item_metadata(offer_item.item_count-1,item)
        if item==selected_item:offer_item.select(offer_item.item_count-1)

    for id in character.get("gear",{}):
        var instance: Dictionary=character.gear[id]
        offer_item.add_item(catalogue.items[instance.item].name+" · Zustand "+str(instance.condition));offer_item.set_item_metadata(offer_item.item_count-1,id)
