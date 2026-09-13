from dataclasses import dataclass


PLAY_LANE_WIDTH = 14.0
SIDE_BAND_WIDTH = 3.0


@dataclass(frozen=True)
class MapPreset:
    name: str
    ground_color: tuple
    roughness: float
    arena_width: float
    arena_length: float
    prop_density: int
    ground_bump: float
    side_theme: str


@dataclass
class ArenaConfig:
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
        return [
            (0.0, 0.0, 0.0),
            (0.0, self.pit_spacing, 0.0),
            (0.0, self.pit_spacing * 2.0, 0.0),
        ]

    @property
    def launch_position(self):
        return (0.0, -self.launch_distance, self.marble_radius)


@dataclass(frozen=True)
class PlayFrame:
    width: float
    length: float
    center_y: float
    half_width: float
    lane_half: float
    side_width: float
    y_min: float
    y_max: float
    far_y: float


def play_frame(config, preset):
    length = max(preset.arena_length, config.pit_spacing * 2.0 + 10.0)
    center_y = config.pit_spacing
    half = preset.arena_width * 0.5
    return PlayFrame(
        width=preset.arena_width,
        length=length,
        center_y=center_y,
        half_width=half,
        lane_half=PLAY_LANE_WIDTH * 0.5,
        side_width=SIDE_BAND_WIDTH,
        y_min=center_y - length * 0.5,
        y_max=center_y + length * 0.5,
        far_y=center_y + length * 0.5,
    )


def _shared_preset(name, color, roughness, bump, density, theme):
    return MapPreset(
        name=name,
        ground_color=color,
        roughness=roughness,
        arena_width=PLAY_LANE_WIDTH + SIDE_BAND_WIDTH * 2.0,
        arena_length=34.0,
        prop_density=density,
        ground_bump=bump,
        side_theme=theme,
    )


MAP_PRESETS = {
    "beach": _shared_preset("Beach", (0.74, 0.48, 0.22, 1.0), 0.82, 0.035, 12, "ocean"),
    "grass": _shared_preset("Grass Field", (0.14, 0.34, 0.08, 1.0), 0.93, 0.025, 16, "meadow"),
    "rough_soil": _shared_preset("Kerala Village Rough Soil", (0.31, 0.135, 0.052, 1.0), 0.96, 0.075, 14, "paddy"),
    "smooth_clay": _shared_preset("Smooth Clay", (0.46, 0.16, 0.07, 1.0), 0.58, 0.008, 8, "curb"),
}
