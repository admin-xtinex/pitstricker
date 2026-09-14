"""Concept-art village side dressing: paddy water and lane fences.
Imported by environment.py. Visual only. Does not create pits or marbles.
"""
import math
import random


def add_lane_side_fences(add_box, wood_mat, collection):
    y0, y1 = -5.2, 28.4
    length = y1 - y0
    mid_y = (y0 + y1) * 0.5
    for side, x in enumerate((-3.45, 3.45)):
        for i in range(25):
            t = i / 24.0
            y = y0 + t * length
            add_box(
                f"LaneFence_Post_{side}_{i}",
                (x, y, 0.62),
                (0.09, 0.09, 1.24),
                wood_mat,
                collection,
            )
        for zi, z in enumerate((0.38, 0.86)):
            add_box(
                f"LaneFence_Rail_{side}_{zi}",
                (x, mid_y, z),
                (0.06, length, 0.06),
                wood_mat,
                collection,
            )


def add_paddy_canals(add_box, add_grass_tuft, water_mat, paddy_mat, reed_mat, collection):
    length = 34.0
    mid_y = 12.0
    for side, sign in enumerate((-1.0, 1.0)):
        add_box(
            f"Paddy_Water_{side}",
            (sign * 5.15, mid_y, -0.06),
            (2.6, length, 0.10),
            water_mat,
            collection,
        )
        add_box(
            f"Paddy_Field_{side}",
            (sign * 8.35, mid_y, 0.02),
            (3.6, length + 2.0, 0.06),
            paddy_mat,
            collection,
        )
        for i in range(40):
            y = -5.0 + i * 0.85 + (0.2 if side else 0.0)
            x = sign * random.uniform(6.6, 9.6)
            add_grass_tuft(
                (x, y, 0.0),
                reed_mat,
                collection,
                f"Paddy_Reed_{side}_{i:02d}",
                random.uniform(0.9, 1.6),
            )
