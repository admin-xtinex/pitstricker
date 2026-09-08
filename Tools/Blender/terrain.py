import random
import bpy
from mathutils import Vector


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.materials,
        bpy.data.cameras,
        bpy.data.lights,
    ):
        # Keep materials: generated material references may be reused during one run.
        if datablocks is bpy.data.materials:
            continue
        for block in list(datablocks):
            if block.users == 0:
                datablocks.remove(block)


def create_ground(config, preset, material):
    center_y = config.pit_spacing
    length = max(preset.arena_length, config.pit_spacing * 2.0 + 10.0)

    bpy.ops.mesh.primitive_cube_add(
        location=(0.0, center_y, -config.ground_thickness / 2.0)
    )
    ground = bpy.context.object
    ground.name = f"Arena_Ground_{preset.name.replace(' ', '_')}"
    ground.dimensions = (preset.arena_width, length, config.ground_thickness)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    ground.data.materials.append(material)
    return ground


def scatter_edge_props(config, preset, material=None):
    """Create lightweight blockout props away from the gameplay center line."""
    random.seed(config.seed)
    collection = bpy.data.collections.new("Environment_Props")
    bpy.context.scene.collection.children.link(collection)

    half_width = preset.arena_width * 0.5
    max_y = config.pit_spacing * 2.0 + 5.0
    min_y = -5.0

    for index in range(preset.prop_density):
        side = -1 if index % 2 == 0 else 1
        x = side * random.uniform(half_width * 0.72, half_width * 0.95)
        y = random.uniform(min_y, max_y)
        scale = random.uniform(0.10, 0.30)

        bpy.ops.mesh.primitive_ico_sphere_add(
            subdivisions=1,
            radius=1.0,
            location=(x, y, scale * 0.55),
        )
        prop = bpy.context.object
        prop.name = f"Edge_Prop_{index:02d}"
        prop.scale = (
            scale * random.uniform(1.0, 2.0),
            scale * random.uniform(0.8, 1.8),
            scale * random.uniform(0.5, 1.1),
        )
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        if material:
            prop.data.materials.append(material)

        for old_collection in list(prop.users_collection):
            old_collection.objects.unlink(prop)
        collection.objects.link(prop)

    return collection


def add_center_guide(config):
    """Thin non-render gameplay reference line through all pit centers."""
    curve_data = bpy.data.curves.new("Pit_Centerline_Data", type="CURVE")
    curve_data.dimensions = "3D"
    curve_data.bevel_depth = 0.008
    curve_data.bevel_resolution = 2

    spline = curve_data.splines.new("POLY")
    spline.points.add(1)
    spline.points[0].co = (0.0, -config.launch_distance, 0.012, 1.0)
    spline.points[1].co = (0.0, config.pit_spacing * 2.0, 0.012, 1.0)

    obj = bpy.data.objects.new("Gameplay_Pit_Centerline", curve_data)
    bpy.context.scene.collection.objects.link(obj)
    obj.hide_render = True
    return obj
