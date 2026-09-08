import math
import random
import bpy
from mathutils import Vector


def _simple_material(name, color, roughness=0.8, metallic=0.0):
    material = bpy.data.materials.get(name) or bpy.data.materials.new(name=name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = metallic
    return material


def _move_to_collection(obj, collection):
    for old_collection in list(obj.users_collection):
        old_collection.objects.unlink(obj)
    collection.objects.link(obj)


def _add_box(name, location, dimensions, material, collection, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if material:
        obj.data.materials.append(material)
    _move_to_collection(obj, collection)
    return obj


def _add_rock(location, scale, material, collection, name, subdivisions=1):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=subdivisions, radius=1.0, location=location)
    rock = bpy.context.object
    rock.name = name
    rock.scale = scale
    rock.rotation_euler[2] = random.uniform(0.0, math.tau)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    rock.data.materials.append(material)
    _move_to_collection(rock, collection)
    return rock


def _add_grass_tuft(location, material, collection, name, scale=1.0):
    x, y, _ = location
    parent = bpy.data.objects.new(name, None)
    parent.empty_display_type = "PLAIN_AXES"
    parent.location = (x, y, 0.0)
    collection.objects.link(parent)

    for blade_index in range(3):
        angle = blade_index * (math.pi / 3.0) + random.uniform(-0.15, 0.15)
        bpy.ops.mesh.primitive_cone_add(
            vertices=4,
            radius1=0.035 * scale,
            radius2=0.0,
            depth=random.uniform(0.16, 0.28) * scale,
            location=(x, y, random.uniform(0.08, 0.13) * scale),
            rotation=(random.uniform(-0.18, 0.18), random.uniform(-0.18, 0.18), angle),
        )
        blade = bpy.context.object
        blade.name = f"{name}_Blade_{blade_index}"
        blade.data.materials.append(material)
        _move_to_collection(blade, collection)
        blade.parent = parent
    return parent


def _add_cylinder_between(name, start, end, radius, material, collection, vertices=10):
    start = Vector(start)
    end = Vector(end)
    direction = end - start
    length = direction.length
    midpoint = (start + end) * 0.5

    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=length,
        location=midpoint,
    )
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    if material:
        obj.data.materials.append(material)
    _move_to_collection(obj, collection)
    return obj


def _add_palm_proxy(location, collection, trunk_material, leaf_material, name, height=4.6):
    x, y, _ = location
    segments = 5
    previous = Vector((x, y, 0.0))
    for i in range(segments):
        t0 = i / segments
        t1 = (i + 1) / segments
        end = Vector((
            x + math.sin(t1 * 1.15) * 0.22,
            y + math.sin(t1 * 0.75) * 0.08,
            height * t1,
        ))
        _add_cylinder_between(
            f"{name}_Trunk_{i}",
            previous,
            end,
            radius=0.12 * (1.0 - t0 * 0.35),
            material=trunk_material,
            collection=collection,
            vertices=12,
        )
        previous = end

    crown = previous
    for i in range(8):
        angle = i * math.tau / 8.0
        length = random.uniform(1.15, 1.7)
        end = crown + Vector((math.cos(angle) * length, math.sin(angle) * length, random.uniform(-0.35, 0.15)))
        _add_cylinder_between(
            f"{name}_LeafStem_{i}", crown, end, 0.035, leaf_material, collection, vertices=6
        )
        for leaflet in range(4):
            t = 0.35 + leaflet * 0.16
            p = crown.lerp(end, t)
            _add_box(
                f"{name}_Leaf_{i}_{leaflet}",
                p,
                (0.38, 0.055, 0.018),
                leaf_material,
                collection,
                rotation=(0.0, random.uniform(-0.12, 0.12), angle),
            )
    return crown


def _add_house(collection, wall_mat, roof_mat, wood_mat):
    # Left-side Kerala-inspired tiled-roof house blockout.
    _add_box("Village_House_Body", (-6.0, 13.0, 1.35), (3.5, 5.2, 2.7), wall_mat, collection)
    _add_box("Village_House_Veranda", (-4.15, 13.0, 0.32), (0.75, 4.9, 0.25), wood_mat, collection)

    roof_z = 3.05
    _add_box(
        "Village_House_Roof_L",
        (-6.78, 13.0, roof_z),
        (2.15, 5.8, 0.20),
        roof_mat,
        collection,
        rotation=(0.0, math.radians(-25.0), 0.0),
    )
    _add_box(
        "Village_House_Roof_R",
        (-5.22, 13.0, roof_z),
        (2.15, 5.8, 0.20),
        roof_mat,
        collection,
        rotation=(0.0, math.radians(25.0), 0.0),
    )

    for y in (11.4, 12.8, 14.2):
        _add_box(f"House_Veranda_Post_{y}", (-4.45, y, 1.25), (0.10, 0.10, 2.2), wood_mat, collection)

    dark = _simple_material("House_Dark", (0.025, 0.018, 0.012, 1.0), 0.85)
    _add_box("House_Door", (-4.23, 13.0, 1.15), (0.04, 0.85, 1.85), dark, collection)
    _add_box("House_Window", (-4.22, 14.55, 1.45), (0.04, 0.8, 0.85), dark, collection)


def _add_stone_wall(collection, stone_mat):
    random.seed(2468)
    for row in range(2):
        z = 0.22 + row * 0.42
        for i in range(18):
            y = 2.0 + i * 1.15 + (0.25 if row else 0.0)
            dims = (
                random.uniform(0.45, 0.72),
                random.uniform(0.85, 1.18),
                random.uniform(0.32, 0.44),
            )
            _add_box(
                f"StoneWall_{row}_{i}",
                (-4.55 + random.uniform(-0.08, 0.08), y, z),
                dims,
                stone_mat,
                collection,
                rotation=(0.0, 0.0, random.uniform(-0.08, 0.08)),
            )


def _add_fence(collection, wood_mat, y=29.2):
    x_values = [-7.0, -5.25, -3.5, -1.75, 0.0, 1.75, 3.5, 5.25, 7.0]
    for i, x in enumerate(x_values):
        _add_box(f"Fence_Post_{i}", (x, y, 0.75), (0.12, 0.12, 1.5), wood_mat, collection)
    for z in (0.55, 1.05):
        _add_box(f"Fence_Rail_{z}", (0.0, y, z), (14.1, 0.10, 0.10), wood_mat, collection)


def _add_bench(collection, wood_mat, stone_mat):
    x, y = 5.7, 7.0
    _add_box("Village_Bench_Seat", (x, y, 0.65), (2.0, 0.55, 0.16), wood_mat, collection)
    _add_box("Village_Bench_Back", (x + 0.82, y, 1.18), (0.14, 0.55, 1.15), wood_mat, collection)
    for dy in (-0.19, 0.19):
        _add_box(f"Village_Bench_Leg_{dy}", (x - 0.62, y + dy, 0.34), (0.18, 0.18, 0.62), stone_mat, collection)
        _add_box(f"Village_Bench_Leg_R_{dy}", (x + 0.62, y + dy, 0.34), (0.18, 0.18, 0.62), stone_mat, collection)


def _add_bicycle(collection, metal_mat):
    x = 6.15
    wheel_y = (9.2, 10.5)
    for idx, y in enumerate(wheel_y):
        bpy.ops.mesh.primitive_torus_add(
            major_radius=0.48,
            minor_radius=0.025,
            major_segments=24,
            minor_segments=6,
            location=(x, y, 0.55),
            rotation=(math.radians(90.0), 0.0, 0.0),
        )
        wheel = bpy.context.object
        wheel.name = f"Bicycle_Wheel_{idx}"
        wheel.data.materials.append(metal_mat)
        _move_to_collection(wheel, collection)

    frame_points = {
        "rear": (x, wheel_y[0], 0.55),
        "front": (x, wheel_y[1], 0.55),
        "seat": (x, 9.62, 1.18),
        "pedal": (x, 9.78, 0.62),
        "bar": (x, 10.22, 1.18),
    }
    bars = [
        ("Bike_Frame_1", "rear", "seat"),
        ("Bike_Frame_2", "seat", "pedal"),
        ("Bike_Frame_3", "pedal", "rear"),
        ("Bike_Frame_4", "pedal", "front"),
        ("Bike_Frame_5", "seat", "front"),
        ("Bike_Fork", "bar", "front"),
    ]
    for name, a, b in bars:
        _add_cylinder_between(name, frame_points[a], frame_points[b], 0.025, metal_mat, collection, vertices=8)


def _add_cow_proxy(collection, cow_mat, dark_mat):
    # Distant readable silhouette only; this is intentionally a low-detail proxy.
    x, y = 5.8, 25.8
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, location=(x, y, 1.05))
    body = bpy.context.object
    body.name = "Cow_Proxy_Body"
    body.scale = (0.9, 1.35, 0.62)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    body.data.materials.append(cow_mat)
    _move_to_collection(body, collection)

    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, location=(x, y - 1.25, 1.28))
    head = bpy.context.object
    head.name = "Cow_Proxy_Head"
    head.scale = (0.48, 0.55, 0.45)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    head.data.materials.append(dark_mat)
    _move_to_collection(head, collection)

    for dx in (-0.48, 0.48):
        for dy in (-0.68, 0.68):
            _add_box(f"Cow_Leg_{dx}_{dy}", (x + dx, y + dy, 0.46), (0.16, 0.16, 0.9), dark_mat, collection)


def _add_hills(collection, hill_mat):
    specs = [(-5.2, 34.0, 3.0, 5.8, 2.6), (0.0, 35.5, 3.4, 7.2, 3.2), (5.8, 34.2, 2.7, 5.2, 2.4)]
    for i, (x, y, z, sx, sz) in enumerate(specs):
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=1.0, location=(x, y, z))
        hill = bpy.context.object
        hill.name = f"Distant_Hill_{i}"
        hill.scale = (sx, 2.6, sz)
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        hill.data.materials.append(hill_mat)
        _move_to_collection(hill, collection)


def _add_village_rough_soil(config, preset, collection):
    random.seed(config.seed + 700)

    grass = _simple_material("Village_Grass", (0.055, 0.26, 0.025, 1.0), 0.93)
    grass_dark = _simple_material("Village_Grass_Dark", (0.025, 0.12, 0.015, 1.0), 0.96)
    stone = _simple_material("Village_Stone", (0.22, 0.20, 0.16, 1.0), 0.95)
    stone_dark = _simple_material("Village_Stone_Dark", (0.10, 0.095, 0.085, 1.0), 0.98)
    wood = _simple_material("Village_Wood", (0.22, 0.085, 0.028, 1.0), 0.87)
    trunk = _simple_material("Palm_Trunk", (0.20, 0.075, 0.018, 1.0), 0.95)
    wall = _simple_material("House_Plaster", (0.48, 0.31, 0.17, 1.0), 0.90)
    roof = _simple_material("Roof_Tile", (0.38, 0.075, 0.025, 1.0), 0.82)
    metal = _simple_material("Bicycle_Metal", (0.035, 0.04, 0.045, 1.0), 0.36, metallic=0.75)
    cow = _simple_material("Cow_Tan", (0.36, 0.17, 0.07, 1.0), 0.82)
    cow_dark = _simple_material("Cow_Dark", (0.09, 0.045, 0.022, 1.0), 0.88)
    hill_mat = _simple_material("Distant_Hill_Material", (0.09, 0.20, 0.085, 1.0), 1.0)

    arena_length = max(preset.arena_length, config.pit_spacing * 2.0 + 10.0)
    center_y = config.pit_spacing

    # Grass verges create the reference-image composition while leaving a clean dirt lane.
    _add_box("Grass_Verge_Left", (-5.8, center_y, 0.025), (4.2, arena_length - 1.0, 0.05), grass, collection)
    _add_box("Grass_Verge_Right", (5.8, center_y, 0.025), (4.2, arena_length - 1.0, 0.05), grass, collection)
    _add_box("Background_Field", (0.0, 29.8, 0.035), (15.2, 5.2, 0.07), grass_dark, collection)

    # Natural grass boundary: dense at edges, sparse near lane.
    for i in range(150):
        side = -1 if i % 2 == 0 else 1
        x = side * random.uniform(3.55, 7.55)
        y = random.uniform(-6.0, 31.0)
        scale = random.uniform(0.75, 1.55)
        _add_grass_tuft((x, y, 0.0), grass if i % 4 else grass_dark, collection, f"Village_Grass_{i:03d}", scale)

    # Pebbles and irregular rocks: mostly edges, with a few safe lane details.
    for i in range(100):
        if i < 78:
            side = -1 if i % 2 == 0 else 1
            x = side * random.uniform(3.0, 7.4)
        else:
            x = random.uniform(-2.6, 2.6)
        y = random.uniform(-5.0, 30.0)
        if any(abs(y - pit_y) < 0.75 for _, pit_y, _ in config.pit_positions):
            continue
        s = random.uniform(0.035, 0.16)
        _add_rock(
            (x, y, s * 0.35),
            (s * random.uniform(1.2, 2.3), s, s * random.uniform(0.45, 0.85)),
            stone if i % 3 else stone_dark,
            collection,
            f"Village_Pebble_{i:03d}",
            subdivisions=1,
        )

    _add_house(collection, wall, roof, wood)
    _add_stone_wall(collection, stone)
    _add_fence(collection, wood)
    _add_bench(collection, wood, stone)
    _add_bicycle(collection, metal)
    _add_cow_proxy(collection, cow, cow_dark)
    _add_hills(collection, hill_mat)

    palms = [
        (-7.0, -2.0, 4.8), (-6.8, 5.2, 5.3), (-7.2, 19.0, 5.0),
        (6.9, -1.0, 5.1), (7.2, 15.5, 5.5), (6.8, 27.0, 4.9),
        (-5.9, 26.5, 4.6), (4.8, 30.2, 4.4),
    ]
    for i, (x, y, h) in enumerate(palms):
        _add_palm_proxy((x, y, 0.0), collection, trunk, grass_dark, f"Village_Palm_{i}", h)


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
        _add_box("Beach_Ocean_Blockout", (0.0, y_max + 5.5, -0.06), (preset.arena_width * 1.8, 7.0, 0.12), water, collection)
        _add_palm_proxy((-half_width + 0.8, 3.0, 0.0), collection, brown, green, "Palm_Left")
        _add_palm_proxy((half_width - 0.8, 18.0, 0.0), collection, brown, green, "Palm_Right")

    elif config.map_name == "grass":
        for i in range(70):
            side = -1 if i % 2 == 0 else 1
            x = side * random.uniform(half_width * 0.70, half_width * 0.97)
            y = random.uniform(y_min, y_max)
            _add_grass_tuft((x, y, 0.0), green, collection, f"Grass_{i:02d}")

    elif config.map_name == "rough_soil":
        _add_village_rough_soil(config, preset, collection)

    elif config.map_name == "smooth_clay":
        clay_trim = _simple_material("Clay_Trim", (0.35, 0.08, 0.025, 1.0), 0.64)
        for side in (-1, 1):
            _add_box(
                f"Clay_Curb_{'L' if side < 0 else 'R'}",
                (side * half_width * 0.88, config.pit_spacing, 0.13),
                (0.22, y_max - y_min, 0.26),
                clay_trim,
                collection,
            )

    return collection
