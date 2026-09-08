import random
import bpy
from materials import make_marker_material


def _simple_material(name, color, roughness=0.8):
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    return material


def _move_to_collection(obj, collection):
    for old_collection in list(obj.users_collection):
        old_collection.objects.unlink(obj)
    collection.objects.link(obj)


def _add_rock(location, scale, material, collection, name):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=location)
    rock = bpy.context.object
    rock.name = name
    rock.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    rock.data.materials.append(material)
    _move_to_collection(rock, collection)


def _add_grass_tuft(location, material, collection, name):
    bpy.ops.mesh.primitive_cone_add(
        vertices=5,
        radius1=0.07,
        radius2=0.0,
        depth=0.20,
        location=(location[0], location[1], 0.10),
    )
    tuft = bpy.context.object
    tuft.name = name
    tuft.data.materials.append(material)
    _move_to_collection(tuft, collection)


def _add_palm_proxy(location, collection, trunk_material, leaf_material, name):
    x, y, _ = location
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=0.11, depth=2.6, location=(x, y, 1.3))
    trunk = bpy.context.object
    trunk.name = f"{name}_Trunk"
    trunk.data.materials.append(trunk_material)
    _move_to_collection(trunk, collection)

    for i in range(6):
        angle = i * 1.0472
        bpy.ops.mesh.primitive_cone_add(
            vertices=5,
            radius1=0.55,
            radius2=0.02,
            depth=0.12,
            location=(x, y, 2.62),
            rotation=(0.0, 0.65, angle),
        )
        leaf = bpy.context.object
        leaf.name = f"{name}_Leaf_{i}"
        leaf.data.materials.append(leaf_material)
        _move_to_collection(leaf, collection)


def add_map_environment(config, preset):
    random.seed(config.seed + 101)
    collection = bpy.data.collections.new(f"Map_Environment_{config.map_name}")
    bpy.context.scene.collection.children.link(collection)

    green = _simple_material("Env_Green", (0.05, 0.26, 0.035, 1.0), 0.85)
    brown = _simple_material("Env_Brown", (0.18, 0.07, 0.025, 1.0), 0.92)
    stone = _simple_material("Env_Stone", (0.18, 0.17, 0.15, 1.0), 0.96)

    half_width = preset.arena_width * 0.5
    y_min = -4.0
    y_max = config.pit_spacing * 2.0 + 4.0

    if config.map_name == "beach":
        water = _simple_material("Water_Blockout", (0.015, 0.24, 0.55, 1.0), 0.25)
        bpy.ops.mesh.primitive_cube_add(location=(0.0, y_max + 5.5, -0.12))
        ocean = bpy.context.object
        ocean.name = "Beach_Ocean_Blockout"
        ocean.dimensions = (preset.arena_width * 1.8, 7.0, 0.12)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        ocean.data.materials.append(water)
        _move_to_collection(ocean, collection)

        _add_palm_proxy((-half_width + 0.8, 3.0, 0.0), collection, brown, green, "Palm_Left")
        _add_palm_proxy((half_width - 0.8, 18.0, 0.0), collection, brown, green, "Palm_Right")

    elif config.map_name == "grass":
        for i in range(70):
            side = -1 if i % 2 == 0 else 1
            x = side * random.uniform(half_width * 0.70, half_width * 0.97)
            y = random.uniform(y_min, y_max)
            _add_grass_tuft((x, y, 0.0), green, collection, f"Grass_{i:02d}")

    elif config.map_name == "rough_soil":
        for i in range(35):
            side = -1 if i % 2 == 0 else 1
            x = side * random.uniform(half_width * 0.68, half_width * 0.96)
            y = random.uniform(y_min, y_max)
            s = random.uniform(0.09, 0.25)
            _add_rock((x, y, s * 0.45), (s * 1.4, s, s * 0.7), stone, collection, f"RoughRock_{i:02d}")

    elif config.map_name == "smooth_clay":
        clay_trim = _simple_material("Clay_Trim", (0.35, 0.08, 0.025, 1.0), 0.64)
        for side in (-1, 1):
            bpy.ops.mesh.primitive_cube_add(location=(side * half_width * 0.88, config.pit_spacing, 0.13))
            curb = bpy.context.object
            curb.name = f"Clay_Curb_{'L' if side < 0 else 'R'}"
            curb.dimensions = (0.22, y_max - y_min, 0.26)
            bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
            curb.data.materials.append(clay_trim)
            _move_to_collection(curb, collection)

    return collection
