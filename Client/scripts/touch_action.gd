extends Button

# Independent screen-touch handling lets an action finger coexist with the joystick.
func _input(event: InputEvent) -> void:
    if event is InputEventScreenTouch and event.pressed and is_visible_in_tree() and not disabled and get_global_rect().has_point(event.position):
        pressed.emit()
        get_viewport().set_input_as_handled()

func _gui_input(event: InputEvent) -> void:
    # Godot also emits a synthetic mouse click for the primary touch. The direct
    # touch handler already fired; consume that click before BaseButton toggles again.
    if event is InputEventMouse and event.device == InputEvent.DEVICE_ID_EMULATION:
        accept_event()
