import bpy
import os
import bmesh
from mathutils import Vector

OUT_DIR = r"C:\Users\Tisan\Documents\pitstricker\pitstricker\Assets\ThirdParty\CanyonDesert"
os.makedirs(OUT_DIR, exist_ok=True)

# Prefer ravine for more height drama, also export a flat canyon bank piece
SOURCE_NAMES = ["1.1b Canyon Ravine", "1.1b Canyon"]

# Crop window in local XY (metres) — a side-bank slab, not full 100x100
# Keep a corridor-shaped chunk ~24m wide x 48m long
CROP = dict(xmin=-12.0, xmax=12.0, ymin=-6.0, ymax=42.0)

def ensure_object_mode():
    if bpy.context.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")


def duplicate_and_crop(src_name, out_name):
    src = bpy.data.objects.get(src_name)
    if src is None:
        print("MISSING", src_name)
        return None

    ensure_object_mode()
    bpy.ops.object.select_all(action="DESELECT")
    src.hide_set(False)
    src.hide_viewport = False
    src.select_set(True)
    bpy.context.view_layer.objects.active = src
    bpy.ops.object.duplicate()
    obj = bpy.context.active_object
    obj.name = out_name

    # Apply scale/rot for clean export
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    # Crop in edit mode by deleting verts outside AABB (local)
    bpy.ops.object.mode_set(mode="EDIT")
    bm = bmesh.from_edit_mesh(obj.data)
    bm.verts.ensure_lookup_table()
    to_delete = []
    for v in bm.verts:
        x, y, z = v.co
        if x < CROP["xmin"] or x > CROP["xmax"] or y < CROP["ymin"] or y > CROP["ymax"]:
            to_delete.append(v)
    bmesh.ops.delete(bm, geom=to_delete, context="VERTS")
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode="OBJECT")

    # Recenter pivot to geometry bounds bottom-center
    bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
    # Put lowest point near z=0
    min_z = min((obj.matrix_world @ v.co).z for v in obj.data.vertices) if len(obj.data.vertices) else 0.0
    obj.location.z -= min_z

    tris = len(obj.data.polygons)
    print(f"CROPPED|{out_name}|verts={len(obj.data.vertices)}|faces={tris}|dims={tuple(round(x,3) for x in obj.dimensions)}")
    return obj


# Clear other objects from export selection later
for name in list(bpy.data.objects.keys()):
    pass

exported = []
# Canyon bank (flatter)
o1 = duplicate_and_crop("1.1b Canyon", "SM_CanyonBank_Crop")
# Ravine wall (taller)
o2 = duplicate_and_crop("1.1b Canyon Ravine", "SM_CanyonRavine_Crop")

# Pack linked images if any
try:
    bpy.ops.file.pack_all()
except Exception as e:
    print("PACK_WARN", e)

# Export each cropped mesh alone
for obj in (o1, o2):
    if obj is None:
        continue
    ensure_object_mode()
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    fbx = os.path.join(OUT_DIR, obj.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=fbx,
        use_selection=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        apply_unit_scale=True,
        bake_space_transform=True,
        path_mode="COPY",
        embed_textures=True,
    )
    print("EXPORTED", fbx)
    exported.append(fbx)

# Also save a small working blend of crops only
crop_blend = os.path.join(OUT_DIR, "CanyonDesert_Crops.blend")
# Delete non-crop meshes for clean save
keep = {o.name for o in (o1, o2) if o is not None}
for obj in list(bpy.data.objects):
    if obj.name not in keep and obj.type == "MESH":
        bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=crop_blend)
print("SAVED", crop_blend)
print("ALL_DONE", len(exported))