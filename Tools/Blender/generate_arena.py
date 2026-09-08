import argparse
import os
import sys
import bpy

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
if SCRIPT_DIR not in sys.path:
    sys.path.insert(0, SCRIPT_DIR)

from config import ArenaConfig, MAP_PRESETS
from materials import make_ground_material, make_marker_material
from terrain import clear_scene, create_ground, scatter_edge_props, add_center_guide
from pits import carve_pits, create_pit_markers
from marbles import create_demo_players
from environment import add_map_environment
from lighting import setup_world_and_lighting
from cameras import create_cameras


def parse_args():
    argv = sys.argv
    argv = argv[argv.index("--") + 1 :] if "--" in argv else []

    parser = argparse.ArgumentParser(description="Generate a Pit Striker Blender arena")
    parser.add_argument("--map", dest="map_name", choices=sorted(MAP_PRESETS.keys()), default="beach")
    parser.add_argument("--pit-spacing", type=float, default=12.0)
    parser.add_argument("--pit-radius", type=float, default=0.18)
    parser.add_argument("--pit-depth", type=float, default=0.12)
    parser.add_argument("--marble-radius", type=float, default=0.16)
    parser.add_argument("--seed", type=int, default=17)
    parser.add_argument("--output", type=str, default="")
    parser.add_argument("--export-glb", type=str, default="")
    return parser.parse_args(argv)


def build_arena(config):
    if config.pit_spacing <= config.pit_radius * 4.0:
        raise ValueError("pit_spacing is too small relative to pit_radius")

    clear_scene()
    preset = MAP_PRESETS[config.map_name]

    ground_material = make_ground_material(
        name=f"Ground_{preset.name.replace(' ', '_')}",
        color=preset.ground_color,
        roughness=preset.roughness,
        bump_strength=preset.ground_bump,
    )
    marker_material = make_marker_material("Pit_Marker_Red", (0.72, 0.025, 0.015, 1.0))

    ground = create_ground(config, preset, ground_material)
    carve_pits(ground, config)
    create_pit_markers(config, marker_material)
    create_demo_players(config)
    scatter_edge_props(config, preset, ground_material)
    add_map_environment(config, preset)
    add_center_guide(config)
    setup_world_and_lighting(config)
    create_cameras(config)

    scene = bpy.context.scene
    scene["pit_striker_map"] = config.map_name
    scene["pit_spacing"] = config.pit_spacing
    scene["pit_radius"] = config.pit_radius
    scene["pit_depth"] = config.pit_depth
    scene["pit_positions"] = str(config.pit_positions)

    print("Pit Striker arena generated")
    print(f"Map: {preset.name}")
    print(f"Pit positions: {config.pit_positions}")
    print(f"Pit spacing: {config.pit_spacing}")
    return scene


def _ensure_parent(path):
    parent = os.path.dirname(os.path.abspath(path))
    if parent:
        os.makedirs(parent, exist_ok=True)


def main():
    args = parse_args()
    config = ArenaConfig(
        map_name=args.map_name,
        pit_spacing=args.pit_spacing,
        pit_radius=args.pit_radius,
        pit_depth=args.pit_depth,
        marble_radius=args.marble_radius,
        seed=args.seed,
    )

    build_arena(config)

    if args.output:
        _ensure_parent(args.output)
        bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(args.output))
        print(f"Saved blend: {os.path.abspath(args.output)}")

    if args.export_glb:
        _ensure_parent(args.export_glb)
        bpy.ops.export_scene.gltf(
            filepath=os.path.abspath(args.export_glb),
            export_format="GLB",
            export_apply=True,
        )
        print(f"Exported GLB: {os.path.abspath(args.export_glb)}")


if __name__ == "__main__":
    main()
