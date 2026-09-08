from dataclasses import dataclass


@dataclass(frozen=True)
class MapPreset:
    name: str
    ground_color: tuple
    roughness: float
    arena_width: float
    arena_length: float
    prop_density: int
    ground_bump: float


@dataclass
class ArenaConfig:
    # Blender UI runs now default to the Kerala-village / rough-soil target.
    map_name: str = "rough_soil"
    pit_radius: float = 0.18
    pit_depth: float = 0.12
    pit_spacing: float = 12.0
    ground_thickness: float = 0.45
    launch_distance: float = 4.0
    marble_radius: float = 0.16
    seed: int = 17

    @property
    def pit_positions(self):
        # Gameplay invariant: all three pits are mathematically collinear.
        return [
            (0.0, 0.0, 0.0),
            (0.0, self.pit_spacing, 0.0),
            (0.0, self.pit_spacing * 2.0, 0.0),
        ]

    @property
    def launch_position(self):
        return (0.0, -self.launch_distance, self.marble_radius)


MAP_PRESETS = {
    "beach": MapPreset(
        name="Beach",
        ground_color=(0.74, 0.48, 0.22, 1.0),
        roughness=0.82,
        arena_width=13.0,
        arena_length=34.0,
        prop_density=18,
        ground_bump=0.035,
    ),
    "grass": MapPreset(
        name="Grass Field",
        ground_color=(0.14, 0.34, 0.08, 1.0),
        roughness=0.93,
        arena_width=13.0,
        arena_length=34.0,
        prop_density=30,
        ground_bump=0.025,
    ),
    "rough_soil": MapPreset(
        name="Kerala Village Rough Soil",
        ground_color=(0.31, 0.135, 0.052, 1.0),
        roughness=0.96,
        arena_width=16.0,
        arena_length=42.0,
        prop_density=18,
        ground_bump=0.075,
    ),
    "smooth_clay": MapPreset(
        name="Smooth Clay",
        ground_color=(0.46, 0.16, 0.07, 1.0),
        roughness=0.58,
        arena_width=13.0,
        arena_length=34.0,
        prop_density=10,
        ground_bump=0.008,
    ),
}
