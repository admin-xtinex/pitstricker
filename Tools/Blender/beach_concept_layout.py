"""Concept-art beach side dressing: ocean, wet sand, driftwood rails.
Imported by environment.py. Visual only. Does not create pits or marbles.
Gameplay frame stays 20 x 34, pits at 0 / 12 / 24, r=0.18.
Left bank stays low for camera travel.
"""
import math
import random


def add_driftwood_rails(add_box, wood_mat, collection):
    y0, y1 = -5.2, 28.4
    length = y1 - y0
    mid_y = (y0 + y1) * 0.5
    for side, x in enumerate((-3.55, 3.55)):
        for i in range(18):
            t = i / 17.0
            y = y0 + t * length
            add_box(
                f"Driftwood_Post_{side}_{i}",
                (x, y, 0.28),
                (0.22, 0.55, 0.22),
                wood_mat,
                collection,
                rotation=(0.0, 0.0, random.uniform(-0.18, 0.18)),
            )
        add_box(
            f"Driftwood_Rail_{side}",
            (x, mid_y, 0.42),
            (0.18, length, 0.18),
            wood_mat,
            collection,
        )


def add_ocean_and_wet_sand(add_box, water_mat, wet_mat, foam_mat, dune_mat, collection):
    length = 34.0
    mid_y = 12.0
    # Right lagoon — camera lives on the left, so the sea reads in frame.
    add_box("Ocean_Right", (7.4, mid_y, -0.10), (5.2, length + 4.0, 0.12), water_mat, collection)
    add_box("WetSand_Right", (4.55, mid_y, 0.01), (1.6, length, 0.04), wet_mat, collection)
    add_box("Foam_Right", (5.35, mid_y, 0.02), (0.35, length, 0.03), foam_mat, collection)
    # Far ocean beyond Pit 3.
    add_box("Ocean_Far", (2.4, 36.5, -0.12), (22.0, 12.0, 0.14), water_mat, collection)
    add_box("Foam_Far", (1.2, 30.6, 0.02), (16.0, 0.55, 0.03), foam_mat, collection)
    # Left dunes stay low so the camera can travel the reserved bank.
    add_box("Dune_Left", (-7.6, mid_y, 0.18), (4.4, length + 2.0, 0.36), dune_mat, collection)
