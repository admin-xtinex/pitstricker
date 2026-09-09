"""
Pit Striker — Build Gameplay Kit (+ Beach placeholders)
Blender 3.2+  |  Run from Scripting workspace → Run Script

Creates:
  - 4 glass-style marbles (Blue/Red/Green/Amber) Ø 0.5 m
  - 3 pit bowls (rim Ø 1.0 m, depth 0.28 m) at Y = 3 / 16.5 / 31
  - 3 red numbered flags
  - chalk launch ring at Y = -6
  - beach sand strip + simple prop placeholders (palm, hut, boat, ocean)

Unity match: SandboxBuilder greybox (Blender Y == Unity Z for layout).
"""

import bpy
import bmesh
import math
from mathutils import Vector

# ---------------------------------------------------------------------------
# Config (Unity SandboxBuilder)
# ---------------------------------------------------------------------------
MARBLE_DIAMETER = 0.5
MARBLE_RADIUS = MARBLE_DIAMETER * 0.5
PIT_RIM_RADIUS = 0.50
PIT_FLOOR_RADIUS = 0.38
PIT_DEPTH = 0.28
PIT_Y = (3.0, 16.5, 31.0)
LAUNCH_Y = -6.0
MARBLE_X = (-0.6, -0.2, 0.2, 0.6)
MARBLE_COLORS = {
    "Blue": (0.05, 0.25, 0.95, 1.0),
    "Red": (0.90, 0.08, 0.08, 1.0),
    "Green": (0.05, 0.75, 0.20, 1.0),
    "Amber": (0.95, 0.55, 0.05, 1.0),
}
FAIRWAY_WIDTH = 2.8
FAIRWAY_LENGTH = 48.0
FAIRWAY_CENTER_Y = 15.0


def _set_units():
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0
    scene.unit_settings.length_unit = "METERS"


def _purge_previous():
    """Remove objects from a previous run of this script."""
    prefixes = (
        "SM_Marble_",
        "SM_Pit_",
        "SM_PitFlag_",
        "SM_ChalkRing",
        "SM_Beach_",
        "PS_Kit_",
        "PS_Light_",
    )
    to_delete = [o for o in bpy.data.objects if o.name.startswith(prefixes)]
    for o in to_delete:
        bpy.data.objects.remove(o, do_unlink=True)
    # Also remove leftover default Cube if present
    if "Cube" in bpy.data.objects:
        bpy.data.objects.remove(bpy.data.objects["Cube"], do_unlink=True)


def _link(obj, collection_name="PitStriker_Kit"):
    if collection_name not in bpy.data.collections:
        col = bpy.data.collections.new(collection_name)
        bpy.context.scene.collection.children.link(col)
    else:
        col = bpy.data.collections[collection_name]
    # unlink from all, link to ours
    for c in list(obj.users_collection):
        c.objects.unlink(obj)
    col.objects.link(obj)
    return obj


def _apply_object_scale(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.select_set(False)


def _glass_material(name, color):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    nt = mat.node_tree
    nodes = nt.nodes
    links = nt.links
    nodes.clear()
    out = nodes.new("ShaderNodeOutputMaterial")
    out.location = (300, 0)
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.location = (0, 0)
    bsdf.inputs["Base Color"].default_value = color
    # Blender 3.2 Principled inputs
    if "Transmission" in bsdf.inputs:
        bsdf.inputs["Transmission"].default_value = 0.85
    if "Transmission Weight" in bsdf.inputs:  # Blender 4+
        bsdf.inputs["Transmission Weight"].default_value = 0.85
    bsdf.inputs["Roughness"].default_value = 0.05
    if "IOR" in bsdf.inputs:
        bsdf.inputs["IOR"].default_value = 1.45
    if "Specular" in bsdf.inputs:
        bsdf.inputs["Specular"].default_value = 0.6
    if "Alpha" in bsdf.inputs:
        bsdf.inputs["Alpha"].default_value = 0.92
    links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    mat.blend_method = "HASHED" if hasattr(mat, "blend_method") else mat.blend_method
    return mat


def _diffuse_material(name, color, roughness=0.7):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is None:
        mat.node_tree.nodes.clear()
        bsdf = mat.node_tree.nodes.new("ShaderNodeBsdfPrincipled")
        out = mat.node_tree.nodes.new("ShaderNodeOutputMaterial")
        mat.node_tree.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness
    return mat


def create_marble(color_name, x, y):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=32,
        ring_count=16,
        radius=MARBLE_RADIUS,
        location=(x, y, MARBLE_RADIUS),
    )
    obj = bpy.context.active_object
    obj.name = f"SM_Marble_{color_name}"
    if obj.data:
        obj.data.name = f"SM_Marble_{color_name}_Mesh"
    mat = _glass_material(f"M_Marble_{color_name}", MARBLE_COLORS[color_name])
    if obj.data.materials:
        obj.data.materials[0] = mat
    else:
        obj.data.materials.append(mat)
    _link(obj)
    return obj


def create_pit(index, y):
    """Cylinder cup: rim R=0.5, floor R=0.38, depth 0.28, rim at Z=0."""
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=32,
        radius=PIT_RIM_RADIUS,
        depth=PIT_DEPTH,
        location=(0.0, y, -PIT_DEPTH * 0.5),
    )
    obj = bpy.context.active_object
    obj.name = f"SM_Pit_Beach_{index:02d}"
    if obj.data:
        obj.data.name = obj.name + "_Mesh"

    # Edit: inset top face to form basin
    bpy.ops.object.mode_set(mode="EDIT")
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()
    # Find top face (highest average Z in local space)
    top = max(bm.faces, key=lambda f: f.calc_center_median().z)
    for f in bm.faces:
        f.select = f is top
    bmesh.update_edit_mesh(obj.data)
    inset_thickness = PIT_RIM_RADIUS - PIT_FLOOR_RADIUS  # 0.12
    bpy.ops.mesh.inset(thickness=inset_thickness, depth=0.0)
    # Push inset face slightly down for flatter floor feel
    bpy.ops.transform.translate(value=(0, 0, -0.02))
    bpy.ops.object.mode_set(mode="OBJECT")

    sand = _diffuse_material("M_Beach_Sand", (0.76, 0.62, 0.38, 1.0), 0.85)
    dark = _diffuse_material("M_Pit_Dark", (0.22, 0.16, 0.10, 1.0), 0.9)
    obj.data.materials.clear()
    obj.data.materials.append(sand)
    obj.data.materials.append(dark)
    # Assign dark to inset-ish faces (lower Z)
    for poly in obj.data.polygons:
        cen = obj.matrix_world @ poly.center
        if cen.z < -0.05:
            poly.material_index = 1
        else:
            poly.material_index = 0

    _link(obj)
    return obj


def create_flag(index, y):
    # Pole
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=12,
        radius=0.015,
        depth=0.70,
        location=(PIT_RIM_RADIUS + 0.35, y, 0.35),
    )
    pole = bpy.context.active_object
    pole.name = f"SM_PitFlag_{index:02d}_Pole"
    wood = _diffuse_material("M_Flag_Pole", (0.35, 0.22, 0.10, 1.0), 0.8)
    pole.data.materials.append(wood)

    # Flag cloth (thin box)
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=(PIT_RIM_RADIUS + 0.35 + 0.12, y, 0.62),
    )
    flag = bpy.context.active_object
    flag.name = f"SM_PitFlag_{index:02d}_Cloth"
    flag.scale = (0.18, 0.01, 0.12)
    bpy.ops.object.transform_apply(scale=True)
    red = _diffuse_material("M_Flag_Red", (0.85, 0.05, 0.05, 1.0), 0.45)
    flag.data.materials.append(red)

    # Number as text
    bpy.ops.object.text_add(location=(PIT_RIM_RADIUS + 0.35 + 0.13, y - 0.02, 0.62))
    txt = bpy.context.active_object
    txt.name = f"SM_PitFlag_{index:02d}_Num"
    txt.data.body = str(index)
    txt.data.size = 0.14
    txt.data.align_x = "CENTER"
    txt.data.align_y = "CENTER"
    txt.rotation_euler[0] = math.radians(90)
    txt.rotation_euler[2] = math.radians(90)
    white = _diffuse_material("M_Flag_Number", (1, 1, 1, 1), 0.5)
    if txt.data.materials:
        txt.data.materials[0] = white
    else:
        txt.data.materials.append(white)
    # Convert text to mesh for export-friendliness
    bpy.context.view_layer.objects.active = txt
    bpy.ops.object.convert(target="MESH")

    # Join pole + cloth + number
    for o in (pole, flag, txt):
        o.select_set(True)
    bpy.context.view_layer.objects.active = pole
    bpy.ops.object.join()
    joined = bpy.context.active_object
    joined.name = f"SM_PitFlag_{index:02d}"
    _link(joined)
    bpy.ops.object.select_all(action="DESELECT")
    return joined


def create_chalk_ring():
    bpy.ops.mesh.primitive_torus_add(
        major_radius=1.0,
        minor_radius=0.02,
        major_segments=48,
        minor_segments=8,
        location=(0.0, LAUNCH_Y, 0.02),
    )
    obj = bpy.context.active_object
    obj.name = "SM_ChalkRing"
    mat = _diffuse_material("M_Chalk", (0.95, 0.95, 0.95, 1.0), 0.9)
    obj.data.materials.append(mat)
    _link(obj)
    return obj


def create_beach_strip():
    """Simple sand fairway; pit holes are separate SM_Pit meshes."""
    bpy.ops.mesh.primitive_cube_add(
        size=1.0,
        location=(0.0, FAIRWAY_CENTER_Y, -0.05),
    )
    obj = bpy.context.active_object
    obj.name = "SM_Beach_Fairway"
    obj.scale = (FAIRWAY_WIDTH, FAIRWAY_LENGTH, 0.10)
    bpy.ops.object.transform_apply(scale=True)
    sand = _diffuse_material("M_Beach_Sand", (0.76, 0.62, 0.38, 1.0), 0.85)
    if obj.data.materials:
        obj.data.materials[0] = sand
    else:
        obj.data.materials.append(sand)
    _link(obj, "PitStriker_Beach")
    return obj


def create_ocean():
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(12.0, 20.0, -0.15))
    obj = bpy.context.active_object
    obj.name = "SM_Beach_Ocean"
    obj.scale = (30.0, 40.0, 1.0)
    bpy.ops.object.transform_apply(scale=True)
    water = _diffuse_material("M_Ocean", (0.10, 0.45, 0.70, 1.0), 0.15)
    bsdf = water.node_tree.nodes.get("Principled BSDF")
    if bsdf and "Transmission" in bsdf.inputs:
        bsdf.inputs["Transmission"].default_value = 0.5
    obj.data.materials.append(water)
    _link(obj, "PitStriker_Beach")
    return obj


def create_placeholder_palm(name, loc):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=8, radius=0.12, depth=4.0, location=(loc[0], loc[1], 2.0)
    )
    trunk = bpy.context.active_object
    trunk.name = name + "_Trunk"
    trunk.data.materials.append(_diffuse_material("M_Palm_Trunk", (0.40, 0.25, 0.10, 1.0)))
    bpy.ops.mesh.primitive_ico_sphere_add(
        subdivisions=2, radius=1.2, location=(loc[0], loc[1], 4.2)
    )
    crown = bpy.context.active_object
    crown.name = name + "_Crown"
    crown.scale = (1.0, 1.0, 0.45)
    bpy.ops.object.transform_apply(scale=True)
    crown.data.materials.append(_diffuse_material("M_Palm_Leaf", (0.15, 0.45, 0.12, 1.0)))
    trunk.select_set(True)
    crown.select_set(True)
    bpy.context.view_layer.objects.active = trunk
    bpy.ops.object.join()
    joined = bpy.context.active_object
    joined.name = name
    _link(joined, "PitStriker_Beach")
    bpy.ops.object.select_all(action="DESELECT")
    return joined


def create_placeholder_hut():
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-6.0, 22.0, 1.5))
    body = bpy.context.active_object
    body.name = "SM_Beach_Hut_Body"
    body.scale = (3.0, 2.5, 2.0)
    bpy.ops.object.transform_apply(scale=True)
    body.data.materials.append(_diffuse_material("M_Hut_Wood", (0.55, 0.35, 0.18, 1.0)))
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(-6.0, 22.0, 3.0))
    roof = bpy.context.active_object
    roof.name = "SM_Beach_Hut_Roof"
    roof.scale = (3.6, 3.0, 0.35)
    bpy.ops.object.transform_apply(scale=True)
    roof.rotation_euler[0] = math.radians(15)
    roof.data.materials.append(_diffuse_material("M_Hut_Roof", (0.45, 0.25, 0.12, 1.0)))
    body.select_set(True)
    roof.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()
    joined = bpy.context.active_object
    joined.name = "SM_Beach_Hut"
    _link(joined, "PitStriker_Beach")
    bpy.ops.object.select_all(action="DESELECT")
    return joined


def create_placeholder_boat():
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=(5.5, 8.0, 0.35))
    obj = bpy.context.active_object
    obj.name = "SM_Beach_Boat"
    obj.scale = (1.2, 3.5, 0.6)
    bpy.ops.object.transform_apply(scale=True)
    obj.data.materials.append(_diffuse_material("M_Boat", (0.30, 0.18, 0.10, 1.0)))
    _link(obj, "PitStriker_Beach")
    return obj


def _frame_view():
    for area in bpy.context.screen.areas:
        if area.type == "VIEW_3D":
            for space in area.spaces:
                if space.type == "VIEW_3D":
                    space.shading.type = "MATERIAL"
            break


def build():
    _set_units()
    _purge_previous()
    bpy.ops.object.select_all(action="DESELECT")

    names = list(MARBLE_COLORS.keys())
    for i, name in enumerate(names):
        create_marble(name, MARBLE_X[i], LAUNCH_Y)

    for i, y in enumerate(PIT_Y, start=1):
        create_pit(i, y)
        create_flag(i, y)

    create_chalk_ring()
    create_beach_strip()
    create_ocean()
    create_placeholder_palm("SM_Beach_Palm_A", (-4.5, 5.0))
    create_placeholder_palm("SM_Beach_Palm_B", (-5.0, 18.0))
    create_placeholder_palm("SM_Beach_Palm_C", (4.0, 28.0))
    create_placeholder_hut()
    create_placeholder_boat()

    # Sun-ish light for Material Preview
    if "PS_Light_Sun" not in bpy.data.objects:
        light_data = bpy.data.lights.new(name="PS_Light_Sun", type="SUN")
        light_data.energy = 3.0
        light_obj = bpy.data.objects.new(name="PS_Light_Sun", object_data=light_data)
        bpy.context.scene.collection.objects.link(light_obj)
        light_obj.rotation_euler = (math.radians(45), math.radians(20), math.radians(30))

    _frame_view()
    print("[Pit Striker] Gameplay kit + beach placeholders built.")
    print("  Marbles Ø0.5m | Pits at Y 3 / 16.5 / 31 | Launch Y -6")
    print("  Collections: PitStriker_Kit , PitStriker_Beach")
    print("  Next: File → Save, then export kit FBXs for Unity.")


if __name__ == "__main__":
    build()
