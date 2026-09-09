import bpy


def carve_pits(ground, config):
    """Boolean-cut three identical cylindrical pits in a perfect straight line."""
    pit_objects = []

    for index, position in enumerate(config.pit_positions, start=1):
        x, y, _ = position

        bpy.ops.mesh.primitive_cylinder_add(
            vertices=64,
            radius=config.pit_radius,
            depth=config.pit_depth + config.ground_thickness + 0.08,
            location=(x, y, -config.pit_depth * 0.5 + 0.02),
        )
        cutter = bpy.context.object
        cutter.name = f"Pit_{index}_Cutter"

        bevel = cutter.modifiers.new(name="Pit_Rim_Bevel", type="BEVEL")
        bevel.width = min(config.pit_radius * 0.18, 0.04)
        bevel.segments = 3
        bpy.context.view_layer.objects.active = cutter
        bpy.ops.object.modifier_apply(modifier=bevel.name)

        boolean = ground.modifiers.new(name=f"Pit_{index}_Boolean", type="BOOLEAN")
        boolean.operation = "DIFFERENCE"
        boolean.solver = "EXACT"
        boolean.object = cutter
        bpy.context.view_layer.objects.active = ground
        bpy.ops.object.modifier_apply(modifier=boolean.name)

        pit_objects.append((index, position))
        bpy.data.objects.remove(cutter, do_unlink=True)

    return pit_objects


def create_pit_markers(config, material):
    collection = bpy.data.collections.new("Pit_Markers")
    bpy.context.scene.collection.children.link(collection)

    for index, (x, y, _) in enumerate(config.pit_positions, start=1):
        bpy.ops.mesh.primitive_cylinder_add(
            vertices=24,
            radius=0.022,
            depth=0.34,
            location=(x + config.pit_radius * 1.45, y, 0.17),
        )
        pole = bpy.context.object
        pole.name = f"Pit_{index}_Marker"
        pole.data.materials.append(material)

        for old_collection in list(pole.users_collection):
            old_collection.objects.unlink(pole)
        collection.objects.link(pole)

        bpy.ops.object.text_add(
            location=(x + config.pit_radius * 1.45, y, 0.38),
            rotation=(1.5708, 0.0, 0.0),
        )
        label = bpy.context.object
        label.name = f"Pit_{index}_Label"
        label.data.body = str(index)
        label.data.align_x = "CENTER"
        label.data.size = 0.15
        label.data.extrude = 0.01
        label.data.materials.append(material)

        for old_collection in list(label.users_collection):
            old_collection.objects.unlink(label)
        collection.objects.link(label)

    return collection
