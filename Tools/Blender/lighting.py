import math
import bpy
from mathutils import Vector


def _look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_world_and_lighting(config):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100

    # Filmic-ish contrast without hard dependency on one Blender version's look names.
    try:
        scene.view_settings.view_transform = "AgX"
    except Exception:
        pass
    for look in ("AgX - Medium High Contrast", "Medium High Contrast"):
        try:
            scene.view_settings.look = look
            break
        except Exception:
            continue

    scene.render.image_settings.file_format = "PNG"

    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.11, 0.18, 0.30, 1.0)
    bg.inputs["Strength"].default_value = 0.32

    # Low warm sun: long shadows are essential to the Kerala village reference.
    bpy.ops.object.light_add(type="SUN", location=(-8.0, -10.0, 8.0))
    sun = bpy.context.object
    sun.name = "Sun_Golden_Hour"
    sun.data.energy = 2.4
    sun.data.color = (1.0, 0.58, 0.30)
    sun.data.angle = math.radians(7.0)
    sun.rotation_euler = (math.radians(62.0), math.radians(-18.0), math.radians(-38.0))

    # Broad cool sky fill keeps shadows readable without flattening the scene.
    bpy.ops.object.light_add(type="AREA", location=(2.0, config.pit_spacing * 0.85, 9.5))
    fill = bpy.context.object
    fill.name = "Sky_Soft_Fill"
    fill.data.energy = 520.0
    fill.data.color = (0.46, 0.64, 1.0)
    fill.data.shape = "DISK"
    fill.data.size = 10.0
    _look_at(fill, (0.0, config.pit_spacing, 0.0))

    # Warm side bounce for house / stone wall / foreground props.
    bpy.ops.object.light_add(type="AREA", location=(-7.0, 7.0, 4.0))
    bounce = bpy.context.object
    bounce.name = "Warm_Village_Bounce"
    bounce.data.energy = 260.0
    bounce.data.color = (1.0, 0.48, 0.22)
    bounce.data.shape = "RECTANGLE"
    bounce.data.size = 5.0
    bounce.data.size_y = 7.0
    _look_at(bounce, (-3.0, 11.0, 0.8))

    # Contact-shadow-friendly render settings where available.
    try:
        scene.render.engine = "BLENDER_EEVEE_NEXT"
        scene.world.color = (0.02, 0.03, 0.05)
    except Exception:
        pass

    return sun, fill, bounce
