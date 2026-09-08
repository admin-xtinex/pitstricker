import argparse
import os
import sys
import bpy


_REQUIRED_MODULE_FILES = (
    "config.py",
    "materials.py",
    "terrain.py",
    "pits.py",
    "marbles.py",
    "environment.py",
    "lighting.py",
    "cameras.py",
)


def _normalise_path(path):
    if not path:
        return None
    try:
        path = bpy.path.abspath(path)
    except Exception:
        pass
    return os.path.normpath(os.path.abspath(path))


def _candidate_script_dirs():
    """Return likely Tools/Blender locations for CLI and Blender Text Editor runs."""
    candidates = []

    # Normal Python / command-line execution.
    raw_file = globals().get("__file__")
    if raw_file and raw_file not in {"<string>", "<blender_text>"}:
        resolved_file = _normalise_path(raw_file)
        if resolved_file:
            candidates.append(os.path.dirname(resolved_file))

    # Blender Scripting workspace: Run Script from the active Text Editor.
    try:
        space = bpy.context.space_data
        text = getattr(space, "text", None)
        text_filepath = getattr(text, "filepath", "") if text else ""
        if text_filepath:
            resolved_text = _normalise_path(text_filepath)
            if resolved_text:
                candidates.append(os.path.dirname(resolved_text))
    except Exception:
        pass

    # Fallback when the active editor is not the Text Editor anymore.
    try:
        for text_block in bpy.data.texts:
            text_filepath = getattr(text_block, "filepath", "")
            if not text_filepath:
                continue
            if os.path.basename(text_filepath).lower() != "generate_arena.py":
                continue
            resolved_text = _normalise_path(text_filepath)
            if resolved_text:
                candidates.append(os.path.dirname(resolved_text))
    except Exception:
        pass

    # If the .blend is saved inside the repository, try common locations.
    blend_filepath = getattr(bpy.data, "filepath", "")
    if blend_filepath:
        blend_dir = os.path.dirname(_normalise_path(blend_filepath))
        candidates.extend(
            [
                blend_dir,
                os.path.join(blend_dir, "Tools", "Blender"),
                os.path.join(os.path.dirname(blend_dir), "Tools", "Blender"),
            ]
        )

    # Useful for launching Blender from the repository root or Tools/Blender.
    cwd = _normalise_path(os.getcwd())
    candidates.extend(
        [
            cwd,
            os.path.join(cwd, "Tools", "Blender"),
            os.path.join(os.path.dirname(cwd), "Tools", "Blender"),
        ]
    )

    # De-duplicate while preserving priority.
    unique = []
    seen = set()
    for candidate in candidates:
        candidate = _normalise_path(candidate)
        if not candidate or candidate in seen:
            continue
        seen.add(candidate)
        unique.append(candidate)
    return unique


def _is_module_dir(path):
    return bool(path) and all(
        os.path.isfile(os.path.join(path, filename))
        for filename in _REQUIRED_MODULE_FILES
    )


def _resolve_script_dir():
    candidates = _candidate_script_dirs()
    for candidate in candidates:
        if _is_module_dir(candidate):
            return candidate

    checked = "\n  - ".join(candidates) if candidates else "(no usable paths detected)"
    raise ModuleNotFoundError(
        "Pit Striker Blender modules could not be located.\n\n"
        "Open Tools/Blender/generate_arena.py directly from the cloned/downloaded "
        "pitstricker repository and make sure the entire Tools/Blender folder is present.\n\n"
        "Expected sibling files include config.py, terrain.py, pits.py, materials.py, "
        "marbles.py, environment.py, lighting.py and cameras.py.\n\n"
        f"Paths checked:\n  - {checked}"
    )


SCRIPT_DIR = _resolve_script_dir()
if SCRIPT_DIR not in sys.path:
    sys.path.insert(0, SCRIPT_DIR)

print(f"Pit Striker Blender modules: {SCRIPT_DIR}")

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
