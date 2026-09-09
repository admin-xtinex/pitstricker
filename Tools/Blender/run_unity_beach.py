"""Generate Beach arena matching Unity greybox scale, then save + export."""
import os
import sys
import bpy

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
if SCRIPT_DIR not in sys.path:
    sys.path.insert(0, SCRIPT_DIR)

from config import ArenaConfig
from generate_arena import build_arena

ROOT = os.path.abspath(os.path.join(SCRIPT_DIR, "..", ".."))
OUT_DIR = os.path.join(ROOT, "Art", "Generated")
os.makedirs(OUT_DIR, exist_ok=True)

BLEND = os.path.join(OUT_DIR, "pit_striker_beach.blend")
GLB = os.path.join(OUT_DIR, "pit_striker_beach.glb")
FBX = os.path.join(OUT_DIR, "pit_striker_beach.fbx")

# Unity SandboxBuilder-ish scale (playable experience)
config = ArenaConfig(
    map_name="beach",
    pit_radius=0.50,
    pit_depth=0.28,
    pit_spacing=14.0,
    marble_radius=0.25,
    launch_distance=9.0,
    seed=17,
)

build_arena(config)

bpy.ops.wm.save_as_mainfile(filepath=BLEND)
print("Saved:", BLEND)

bpy.ops.export_scene.gltf(filepath=GLB, export_format="GLB", export_apply=True)
print("Exported GLB:", GLB)

bpy.ops.export_scene.fbx(
    filepath=FBX,
    use_selection=False,
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    apply_unit_scale=True,
    bake_space_transform=True,
)
print("Exported FBX:", FBX)
print("DONE")
