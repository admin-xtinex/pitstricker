import math
import bpy
from mathutils import Vector


def _look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def _set_render_engine(scene):
    """Pick the Eevee enum exposed by the current Blender build."""
    for engine in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE"):
        try:
            scene.render.engine = engine
            print(f"Pit Striker render engine: {engine}")
            return engine
        except (TypeError, ValueError):
            continue

    # Last-resort fallback so scene generation still completes.
    try:
        scene.render.engine = "BLENDER_WORKBENCH"
        print("Pit Striker render engine: BLENDER_WORKBENCH (fallback)")
        return "BLENDER_WORKBENCH"
    except Exception as exc:
        raise RuntimeError("No supported Blender render engine could be selected") from exc


def setup_world_and_lighting(config):
    scene = bpy.context.scene
    _set_render_engine(scene)
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"

    # The previous blockout was under-exposed in Blender 5.x. Keep the warm
    # cinematic contrast, but lift overall exposure so grass, dirt and props
    # remain readable in both Rendered viewport and F12 output.
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

    try:
        scene.view_settings.exposure = 1.25
        scene.view_settings.gamma = 1.0
    except Exception:
        pass

    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    # Bright sky fill: blue enough to balance the orange sun, but not night-like.
    bg.inputs["Color"].default_value = (0.30, 0.46, 0.72, 1.0)
    bg.inputs["Strength"].default_value = 0.72

    # Low warm sun for the long late-afternoon shadows seen in the reference.
    bpy.ops.object.light_add(type="SUN", location=(-8.0, -10.0, 8.0))
    sun = bpy.context.object
    sun.name = "Sun_Golden_Hour"
    sun.data.energy = 3.6
    sun.data.color = (1.0, 0.67, 0.40)
    sun.data.angle = math.radians(5.0)
    sun.rotation_euler = (
        math.radians(54.0),
        math.radians(-14.0),
        math.radians(-42.0),
    )

    # Large cool fill from above/front. This is intentionally strong enough to
    # keep the house facade and grass verges visible without flattening shadows.
    bpy.ops.object.light_add(type="AREA", location=(1.5, config.pit_spacing * 0.72, 10.5))
    fill = bpy.context.object
    fill.name = "Sky_Soft_Fill"
    fill.data.energy = 1250.0
    fill.data.color = (0.58, 0.72, 1.0)
    fill.data.shape = "DISK"
    fill.data.size = 13.0
    _look_at(fill, (0.0, config.pit_spacing * 0.85, 0.0))

    # Warm bounce from the house side gives the foreground the same sunlit,
    # earthy feel as the target image.
    bpy.ops.object.light_add(type="AREA", location=(-6.5, 5.0, 4.8))
    bounce = bpy.context.object
    bounce.name = "Warm_Village_Bounce"
    bounce.data.energy = 600.0
    bounce.data.color = (1.0, 0.54, 0.26)
    bounce.data.shape = "RECTANGLE"
    bounce.data.size = 6.0
    bounce.data.size_y = 8.0
    _look_at(bounce, (-2.0, 9.0, 0.9))

    # A subtle front fill prevents the near dirt lane from becoming a black
    # silhouette when the golden-hour sun is behind the scene.
    bpy.ops.object.light_add(type="AREA", location=(0.0, -5.5, 3.0))
    front = bpy.context.object
    front.name = "Gameplay_Front_Fill"
    front.data.energy = 350.0
    front.data.color = (1.0, 0.82, 0.62)
    front.data.shape = "DISK"
    front.data.size = 5.0
    _look_at(front, (0.0, 4.0, 0.0))

    return sun, fill, bounce, front
