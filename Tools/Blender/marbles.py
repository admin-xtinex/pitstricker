import bpy
from materials import make_glass_marble_material


PLAYER_COLORS = {
    "P1": (0.025, 0.18, 0.95, 1.0),
    "P2": (0.80, 0.025, 0.02, 1.0),
    "P3": (0.02, 0.62, 0.09, 1.0),
    "P4": (0.95, 0.58, 0.02, 1.0),
}


def create_marble(name, location, radius, color):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=64,
        ring_count=32,
        radius=radius,
        location=location,
    )
    marble = bpy.context.object
    marble.name = name
    bpy.ops.object.shade_smooth()
    marble.data.materials.append(make_glass_marble_material(f"{name}_Glass", color))

    bevel = marble.modifiers.new(name="Marble_MicroBevel", type="BEVEL")
    bevel.width = radius * 0.025
    bevel.segments = 2
    return marble


def create_demo_players(config):
    marbles = []
    positions = [
        config.launch_position,
        (-1.0, config.pit_spacing * 0.68, config.marble_radius),
        (0.9, config.pit_spacing * 1.18, config.marble_radius),
        (-0.75, config.pit_spacing * 1.72, config.marble_radius),
    ]

    for (player, color), position in zip(PLAYER_COLORS.items(), positions):
        marbles.append(
            create_marble(
                name=f"Marble_{player}",
                location=position,
                radius=config.marble_radius,
                color=color,
            )
        )

    return marbles
