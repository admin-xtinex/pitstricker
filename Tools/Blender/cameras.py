import bpy
from mathutils import Vector


def _look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def _focus_empty(name, location):
    empty = bpy.data.objects.new(name, None)
    empty.empty_display_type = "PLAIN_AXES"
    empty.location = location
    bpy.context.scene.collection.objects.link(empty)
    empty.hide_render = True
    return empty


def create_cameras(config):
    cameras = []

    # Main gameplay camera: very low, nearly centered on the lane, with enough
    # field of view to show Pit 1 prominently while Pit 2 and Pit 3 recede in a
    # perfectly straight line. This composition is intentionally close to the
    # approved Kerala-village reference.
    bpy.ops.object.camera_add(location=(0.55, -5.20, 0.55))
    gameplay = bpy.context.object
    gameplay.name = "Camera_Gameplay"
    gameplay.data.lens = 46
    gameplay.data.sensor_width = 36
    gameplay.data.clip_start = 0.03
    gameplay.data.clip_end = 300.0
    _look_at(gameplay, (0.0, config.pit_spacing * 0.92, 0.18))

    focus = _focus_empty("Camera_Focus_Pit1_Lane", (0.0, 1.25, 0.14))
    gameplay.data.dof.use_dof = True
    gameplay.data.dof.focus_object = focus
    gameplay.data.dof.aperture_fstop = 4.5
    cameras.append(gameplay)

    # Oblique overview for editing and environment inspection.
    bpy.ops.object.camera_add(location=(9.0, 6.0, 8.5))
    overview = bpy.context.object
    overview.name = "Camera_Overview"
    overview.data.lens = 48
    overview.data.clip_end = 300.0
    _look_at(overview, (0.0, config.pit_spacing, 0.0))
    cameras.append(overview)

    # Centered debug camera for checking that all three pit centers remain
    # mathematically collinear after environment changes.
    bpy.ops.object.camera_add(location=(0.0, -6.2, 1.20))
    straight = bpy.context.object
    straight.name = "Camera_Straight_Pit_Debug"
    straight.data.lens = 55
    straight.data.clip_end = 300.0
    _look_at(straight, (0.0, config.pit_spacing, 0.0))
    cameras.append(straight)

    bpy.context.scene.camera = gameplay
    return cameras
