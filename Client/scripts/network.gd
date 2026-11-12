extends Node

signal state_changed(message: String)
signal snapshot_received(players: Array)

var player_id: String = ""
var direction := Vector2.ZERO
var _socket: WebSocketPeer
var _sequence := 0
var _elapsed := 0.0
var _connecting_time := 0.0

func connect_to_server(url: String) -> void:
    disconnect_from_server()
    if not (url.begins_with("ws://") or url.begins_with("wss://")):
        state_changed.emit("Use a ws:// or wss:// server address")
        return
    if OS.has_feature("web") and JavaScriptBridge.eval("window.location.protocol") == "https:" and not url.begins_with("wss://"):
        state_changed.emit("HTTPS requires a wss:// server")
        return
    _socket = WebSocketPeer.new()
    _socket.inbound_buffer_size = 65536
    _socket.outbound_buffer_size = 4096
    _socket.max_queued_packets = 64
    var error := _socket.connect_to_url(url)
    if error != OK:
        _socket = null
        state_changed.emit("Connection could not start (%s)" % error)
    else:
        _connecting_time = 0.0
        state_changed.emit("Connecting…")

func disconnect_from_server() -> void:
    if _socket:
        _socket.close()
    _socket = null
    player_id = ""
    direction = Vector2.ZERO
    _sequence = 0
    _elapsed = 0.0
    snapshot_received.emit([])
    state_changed.emit("Disconnected")

func _process(delta: float) -> void:
    if not _socket:
        return
    _socket.poll()
    var state := _socket.get_ready_state()
    if state == WebSocketPeer.STATE_CLOSED:
        disconnect_from_server()
        state_changed.emit("Connection closed; reconnect to try again")
        return
    if player_id.is_empty():
        _connecting_time += delta
        if _connecting_time > 10.0:
            disconnect_from_server()
            state_changed.emit("Connection timed out")
            return
    if state != WebSocketPeer.STATE_OPEN:
        return
    while _socket.get_available_packet_count() > 0:
        var message = JSON.parse_string(_socket.get_packet().get_string_from_utf8())
        if not message is Dictionary:
            continue
        if message.get("type") == "welcome":
            if message.get("version") != 1 or not message.get("id") is String:
                disconnect_from_server()
                state_changed.emit("Incompatible server protocol")
                return
            player_id = message.id
            state_changed.emit("Connected — WASD, arrows or touch to move")
        elif message.get("type") == "snapshot" and message.get("players") is Array:
            snapshot_received.emit(message.players)
    if player_id.is_empty():
        return
    _elapsed += delta
    if _elapsed >= 0.05:
        _elapsed = 0.0
        _sequence += 1
        var input := direction.limit_length()
        if _socket.send_text(JSON.stringify({"type": "input", "seq": _sequence, "x": input.x, "z": input.y})) != OK:
            disconnect_from_server()
            state_changed.emit("Send failed; reconnect to try again")
