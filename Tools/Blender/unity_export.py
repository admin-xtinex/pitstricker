import os
import bpy


_EXPORTABLE_TYPES = {"MESH", "CURVE", "EMPTY"}


def _move_to_collection(obj, collection):
    for old_collection in list(obj.users_collection):
        old_collection.objects.unlink(obj)
    collection.objects.link(obj)


def create_unity_anchors(config):
    """Create named empties that Unity can use for gameplay positions."""
    old = bpy.data.collections.get("Unity_Anchors")
    if old:
        for obj in list(old.objects):
            bpy.data.objects.remove(obj, do_unlink=True)
        bpy.data.collections.remove(old)

    collection = bpy.data.collections.new("Unity_Anchors")
    bpy.context.scene.collection.children.link(collection)

    anchors = []
    for index, (x, y, z) in enumerate(config.pit_positions, start=1):
        anchor = bpy.data.objects.new(f"PS_Pit_{index}_Anchor", None)
        anchor.empty_display_type = "SPHERE"
        anchor.empty_display_size = max(config.pit_radius, 0.12)
        anchor.location = (x, y, z)
        anchor["pit_index"] = index
        anchor["pit_radius"] = config.pit_radius
        anchor["pit_depth"] = config.pit_depth
        collection.objects.link(anchor)
        anchors.append(anchor)

    spawn = bpy.data.objects.new("PS_Player_Spawn", None)
    spawn.empty_display_type = "ARROWS"
    spawn.empty_display_size = 0.35
    spawn.location = config.launch_position
    spawn["role"] = "player_spawn"
    collection.objects.link(spawn)
    anchors.append(spawn)

    arena_origin = bpy.data.objects.new("PS_Arena_Origin", None)
    arena_origin.empty_display_type = "PLAIN_AXES"
    arena_origin.empty_display_size = 0.5
    arena_origin.location = (0.0, 0.0, 0.0)
    arena_origin["pit_spacing"] = config.pit_spacing
    arena_origin["map_name"] = config.map_name
    collection.objects.link(arena_origin)
    anchors.append(arena_origin)

    return anchors


def default_unity_fbx_path(script_dir, map_name):
    """Place generated FBX inside the real Unity project folder."""
    repo_root = os.path.abspath(os.path.join(script_dir, "..", ".."))
    models_dir = os.path.join(
        repo_root,
        "pitstricker",
        "Assets",
        "_Project",
        "Art",
        "Environments",
        "Village",
        "Generated",
    )
    os.makedirs(models_dir, exist_ok=True)
    safe_name = map_name.replace(" ", "_").replace("-", "_")
    return os.path.join(models_dir, f"PitStriker_{safe_name}.fbx")


def _should_export(obj, include_debug=False):
    if obj.type not in _EXPORTABLE_TYPES:
        return False

    name = obj.name
    if name.startswith("Camera_") or name.startswith("Sun_"):
        return False
    if name in {"Sky_Soft_Fill", "Warm_Village_Bounce", "Gameplay_Front_Fill"}:
        return False
    if name.startswith("Camera_Focus_"):
        return False
    if name == "Gameplay_Pit_Centerline":
        return False

    if not include_debug:
        if name.startswith("Pit_") and (name.endswith("_Marker") or name.endswith("_Label")):
            return False

    return True


def _convert_curves_to_mesh():
    """Blender 5 FBX exporter no longer accepts CURVE object_types."""
    curves = [obj for obj in bpy.context.scene.objects if obj.type == "CURVE"]
    if not curves:
        return 0

    bpy.ops.object.select_all(action="DESELECT")
    converted = 0
    for obj in curves:
        try:
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.convert(target="MESH")
            converted += 1
        except Exception as exc:
            print(f"WARNING: could not convert curve {obj.name} to mesh: {exc}")
        finally:
            obj.select_set(False)
    print(f"Converted {converted} curve object(s) to mesh for FBX")
    return converted


def _fbx_object_types():
    version = getattr(bpy.app, "version", (4, 0, 0))
    if version[0] >= 5:
        return {"MESH", "EMPTY", "OTHER"}
    return {"MESH", "CURVE", "EMPTY"}


def export_unity_fbx(filepath, include_debug=False):
    """Export generated arena to Unity-friendly FBX coordinates."""
    filepath = os.path.abspath(filepath)
    os.makedirs(os.path.dirname(filepath), exist_ok=True)

    _convert_curves_to_mesh()

    bpy.ops.object.select_all(action="DESELECT")
    export_objects = []
    for obj in bpy.context.scene.objects:
        if _should_export(obj, include_debug=include_debug):
            obj.select_set(True)
            export_objects.append(obj)

    if not export_objects:
        raise RuntimeError("No Unity-exportable objects found in the Blender scene")

    bpy.context.view_layer.objects.active = export_objects[0]
    object_types = _fbx_object_types()
    print(f"FBX object_types={sorted(object_types)} blender={bpy.app.version_string}")

    try:
        bpy.ops.export_scene.fbx(
            filepath=filepath,
            use_selection=True,
            apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_UNITS",
            object_types=object_types,
            use_mesh_modifiers=True,
            mesh_smooth_type="FACE",
            axis_forward="-Z",
            axis_up="Y",
            bake_anim=False,
            add_leaf_bones=False,
            path_mode="AUTO",
            embed_textures=False,
        )
    except Exception as exc:
        raise RuntimeError(
            "FBX export failed. Make sure Blender's FBX exporter is available. "
            f"Original error: {exc}"
        ) from exc

    if not os.path.isfile(filepath):
        raise RuntimeError(f"FBX operator returned but file is missing: {filepath}")

    print(f"Unity FBX exported: {filepath}")
    return filepath
