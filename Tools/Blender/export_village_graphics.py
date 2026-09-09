"""Export supplied village scenery only; gameplay remains owned by Unity."""
import sys
import json
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).parent))
sys.path.insert(0, str(Path(__file__).parent / 'VillageSource'))
from village_reference_detail import build

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
build()

# Bake world transforms. The detailed scenery uses the existing Unity course
# dimensions and contains no gameplay anchors or collision meshes.
for obj in list(bpy.context.scene.objects):
    world = obj.matrix_world.copy()
    obj.parent = None
    obj.matrix_world = world
    if obj.type == 'MESH':
        for vertex in obj.data.vertices:
            p = obj.matrix_world @ vertex.co
            vertex.co = p
        obj.matrix_world.identity()

out = ROOT / 'pitstricker/Assets/_Project/Art/Environments/Village'
out.mkdir(parents=True, exist_ok=True)
palette = []
for mat in bpy.data.materials:
    node = mat.node_tree.nodes.get('Principled BSDF') if mat.use_nodes else None
    if node:
        palette.append(dict(name=mat.name, color=list(node.inputs['Base Color'].default_value),
                            roughness=node.inputs['Roughness'].default_value,
                            metallic=node.inputs['Metallic'].default_value))
(out / 'palette.json').write_text(json.dumps(dict(materials=palette), indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT / 'Art/Generated/VillageIntegration/village_graphics.blend'))
bpy.ops.export_scene.fbx(filepath=str(out / 'Village_Graphics.fbx'),
    object_types={'MESH'}, use_mesh_modifiers=True, bake_anim=False,
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True)
print('VILLAGE_GRAPHICS_EXPORT_OK')
