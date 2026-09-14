"""Full beach dressing for generate_arena --map beach. Visual only."""
import random


def add_beach_shore(config, preset, collection):
    from environment import (
        _simple_material,
        _add_box,
        _add_rock,
        _add_grass_tuft,
        _add_palm_proxy,
        _move_to_collection,
    )
    import bpy

    random.seed(config.seed + 900)

    sand = _simple_material("Beach_Sand", (0.78, 0.58, 0.32, 1.0), 0.86)
    wet = _simple_material("Beach_WetSand", (0.52, 0.38, 0.22, 1.0), 0.42)
    water = _simple_material("Beach_Ocean", (0.04, 0.38, 0.55, 1.0), 0.12)
    foam = _simple_material("Beach_Foam", (0.92, 0.95, 0.96, 1.0), 0.35)
    dune = _simple_material("Beach_Dune", (0.70, 0.52, 0.28, 1.0), 0.92)
    wood = _simple_material("Beach_Driftwood", (0.28, 0.16, 0.08, 1.0), 0.90)
    thatch = _simple_material("Beach_Thatch", (0.42, 0.28, 0.10, 1.0), 0.88)
    plaster = _simple_material("Beach_HutPlaster", (0.82, 0.72, 0.52, 1.0), 0.80)
    trunk = _simple_material("Palm_Trunk_Beach", (0.28, 0.14, 0.05, 1.0), 0.94)
    leaf = _simple_material("Palm_Leaf_Beach", (0.10, 0.38, 0.08, 1.0), 0.78)
    white = _simple_material("Lighthouse_White", (0.92, 0.90, 0.84, 1.0), 0.55)
    red = _simple_material("Lighthouse_Stripe", (0.62, 0.12, 0.08, 1.0), 0.55)
    rock = _simple_material("Beach_Rock", (0.32, 0.30, 0.26, 1.0), 0.95)
    grass = _simple_material("Beach_Grass", (0.22, 0.38, 0.08, 1.0), 0.90)

    arena_length = max(preset.arena_length, config.pit_spacing * 2.0 + 10.0)
    center_y = config.pit_spacing

    _add_box("Sand_Verge_Left", (-5.9, center_y, 0.025), (4.4, arena_length - 1.0, 0.05), sand, collection)
    _add_box("Sand_Verge_Right", (5.4, center_y, 0.025), (3.2, arena_length - 1.0, 0.05), sand, collection)
    _add_box("Dune_Left", (-7.6, center_y, 0.18), (4.4, arena_length + 2.0, 0.36), dune, collection)
    _add_box("WetSand_Right", (4.55, center_y, 0.01), (1.6, arena_length, 0.04), wet, collection)
    _add_box("Ocean_Right", (7.6, center_y, -0.10), (5.6, arena_length + 4.0, 0.12), water, collection)
    _add_box("Foam_Right", (5.40, center_y, 0.02), (0.38, arena_length, 0.03), foam, collection)
    _add_box("Ocean_Far", (2.2, 36.5, -0.12), (24.0, 12.0, 0.14), water, collection)
    _add_box("Foam_Far", (1.0, 30.6, 0.02), (18.0, 0.55, 0.03), foam, collection)
    _add_box("Background_Sand", (0.0, 29.8, 0.03), (16.0, 5.0, 0.06), sand, collection)

    y0, y1 = -5.2, 28.4
    rail_len = y1 - y0
    mid_y = (y0 + y1) * 0.5
    for side, x in enumerate((-3.55, 3.55)):
        for i in range(18):
            t = i / 17.0
            y = y0 + t * rail_len
            _add_box(
                f"Driftwood_Post_{side}_{i}",
                (x, y, 0.28),
                (0.22, 0.55, 0.22),
                wood,
                collection,
                rotation=(0.0, 0.0, random.uniform(-0.18, 0.18)),
            )
        _add_box(f"Driftwood_Rail_{side}", (x, mid_y, 0.42), (0.18, rail_len, 0.18), wood, collection)

    for i in range(80):
        side = -1 if i % 2 == 0 else 1
        x = side * random.uniform(3.6, 7.6)
        y = random.uniform(-6.0, 31.0)
        if side < 0:
            _add_grass_tuft((x, y, 0.0), grass, collection, f"Dune_Grass_{i:03d}", random.uniform(0.7, 1.4))
        else:
            s = random.uniform(0.04, 0.14)
            _add_rock(
                (x, y, s * 0.3),
                (s * random.uniform(1.1, 2.0), s, s * random.uniform(0.4, 0.8)),
                rock,
                collection,
                f"Beach_Pebble_{i:03d}",
            )

    _add_box("Beach_Hut_Body", (7.5, 8.2, 1.15), (3.2, 4.2, 2.3), plaster, collection)
    _add_box("Beach_Hut_Roof", (7.5, 8.2, 2.55), (3.8, 4.8, 0.55), thatch, collection)
    _add_box("Beach_Hut_Deck", (6.2, 8.2, 0.22), (1.2, 4.0, 0.12), wood, collection)
    _add_box("Beach_Hut_Post_A", (5.75, 6.6, 0.85), (0.12, 0.12, 1.5), wood, collection)
    _add_box("Beach_Hut_Post_B", (5.75, 9.8, 0.85), (0.12, 0.12, 1.5), wood, collection)
    _add_box("Beach_Boat_Hull", (8.2, 22.0, 0.28), (1.15, 3.6, 0.55), wood, collection)
    _add_box("Beach_Boat_Cabin", (8.2, 21.45, 0.72), (0.72, 1.1, 0.50), plaster, collection)
    _add_box("Lighthouse_Island", (9.6, 33.5, 0.35), (3.4, 3.4, 0.70), rock, collection)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.55, depth=6.4, location=(9.6, 33.5, 3.4))
    shaft = bpy.context.object
    shaft.name = "Lighthouse_Shaft"
    shaft.data.materials.append(white)
    _move_to_collection(shaft, collection)
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=0.58, depth=1.1, location=(9.6, 33.5, 3.6))
    stripe = bpy.context.object
    stripe.name = "Lighthouse_Stripe"
    stripe.data.materials.append(red)
    _move_to_collection(stripe, collection)
    _add_box("Lighthouse_Lamp", (9.6, 33.5, 6.8), (1.3, 1.3, 0.70), foam, collection)

    palms = [
        (-7.2, -2.0, 4.8), (-7.4, 5.4, 5.2), (-7.8, 18.5, 5.0),
        (-6.6, 27.2, 4.6), (6.6, -1.2, 5.0), (7.4, 15.0, 5.4),
        (6.8, 26.6, 4.8), (4.6, 30.4, 4.4),
    ]
    for i, (x, y, h) in enumerate(palms):
        _add_palm_proxy((x, y, 0.0), collection, trunk, leaf, f"Beach_Palm_{i}", h)
