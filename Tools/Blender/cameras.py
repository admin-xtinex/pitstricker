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

    # Main reference-style camera: low to the dirt, slightly off-centre,
    # looking directly down the mathematically straight pit line.
    bpy.ops.object.camera_add(location=(1.65, -5.35, 0.72))
    gameplay = bpy.context.object
    gameplay.name = "Camera_Gameplay"
    gameplay.data.lens = 52
    gameplay.data.sensor_width = 36
    gameplay.data.clip_start = 0.05
    gameplay.data.clip_end = 250.0
    _look_at(gameplay, (0.0, config.pit_spacing * 0.78, 0.28))

    focus = _focus_empty("Camera_Focus_Pit1_Lane", (0.0, 3.8, 0.16))
    gameplay.data.dof.use_dof = True
    gameplay.data.dof.focus_object = focus
    gameplay.data.dof.aperture_fstop = 3.2
    cameras.append(gameplay)

    # Clean oblique overview for scene inspection and layout work.
    bpy.ops.object.camera_add(location=(9.0, 6.0, 8.5))
    overview = bpy.context.object
    overview.name = "Camera_Overview"
    overview.data.lens = 48
    overview.data.clip_end = 250.0
    _look_at(overview, (0.0, config.pit_spacing, 0.0))
    cameras.append(overview)

    # Straight gameplay/debug camera for validating pit collinearity.
    bpy.ops.object.camera_add(location=(0.0, -6.2, 1.35))
    straight = bpy.context.object
    straight.name = "Camera_Straight_Pit_Debug"
    straight.data.lens = 58
    straight.data.clip_end = 250.0
    _look_at(straight, (0.0, config.pit_spacing, 0.0))
    cameras.append(straight)

    bpy.context.scene.camera = gameplay
    return cameras
