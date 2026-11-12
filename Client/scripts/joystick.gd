extends Control

var direction := Vector2.ZERO
var pointer := -1
var mouse_active := false
var knob := Vector2.ZERO
const RADIUS := 68.0

func _ready() -> void:
    mouse_filter = Control.MOUSE_FILTER_IGNORE
    custom_minimum_size = Vector2(210, 210)

func _draw() -> void:
    var center := size/2
    draw_circle(center, 88, Color(.03,.10,.14,.48))
    draw_arc(center, 86, 0, TAU, 64, Color(.52,.81,.78,.65), 2, true)
    draw_arc(center, 62, 0, TAU, 48, Color(.7,.85,.8,.15), 1, true)
    draw_circle(center + knob, 31, Color(.43,.85,.74,.75))
    draw_arc(center + knob, 31, 0, TAU, 32, Color(.8,1,.93,.85), 2, true)
    draw_line(center+Vector2(-8,0),center+Vector2(8,0),Color(.05,.22,.25),2)
    draw_line(center+Vector2(0,-8),center+Vector2(0,8),Color(.05,.22,.25),2)

func _update_pointer(pos: Vector2) -> void:
    knob = (pos - get_global_rect().get_center()).limit_length(RADIUS)
    direction = knob / RADIUS if knob.length() > 8 else Vector2.ZERO
    queue_redraw()

func reset() -> void:
    pointer = -1
    mouse_active = false
    knob = Vector2.ZERO
    direction = Vector2.ZERO
    queue_redraw()

func _notification(what: int) -> void:
    if what == NOTIFICATION_APPLICATION_FOCUS_OUT or what == NOTIFICATION_PAUSED:
        reset()

func _input(event: InputEvent) -> void:
    if not is_visible_in_tree():
        reset()
        return
    if event is InputEventScreenTouch:
        if event.pressed and pointer == -1 and get_global_rect().has_point(event.position):
            pointer = event.index
            _update_pointer(event.position)
            get_viewport().set_input_as_handled()
        elif event.index == pointer and not event.pressed:
            reset()
            get_viewport().set_input_as_handled()
    elif event is InputEventScreenDrag and event.index == pointer:
        _update_pointer(event.position)
        get_viewport().set_input_as_handled()
    elif event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
        if event.pressed and get_global_rect().has_point(event.position):
            mouse_active = true
            _update_pointer(event.position)
            get_viewport().set_input_as_handled()
        elif not event.pressed and mouse_active:
            reset()
            get_viewport().set_input_as_handled()
    elif event is InputEventMouseMotion and mouse_active:
        _update_pointer(event.position)
        get_viewport().set_input_as_handled()
