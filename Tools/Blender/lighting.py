import bpy


def setup_world_and_lighting(config):
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 1920
    scene.render.resolution_y = 1080
    scene.render.resolution_percentage = 100

    world = bpy.data.worlds.get("World") or bpy.data.worlds.new("World")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.14, 0.28, 0.52, 1.0)
    bg.inputs["Strength"].default_value = 0.45

    bpy.ops.object.light_add(type="SUN", location=(4.0, -6.0, 9.0))
    sun = bpy.context.object
    sun.name = "Sun_Key"
    sun.rotation_euler = (0.62, -0.28, -0.55)
    sun.data.energy = 3.0
    sun.data.angle = 0.08

    bpy.ops.object.light_add(type="AREA", location=(-4.0, config.pit_spacing * 0.8, 7.0))
    fill = bpy.context.object
    fill.name = "Soft_Fill"
    fill.data.energy = 900.0
    fill.data.shape = "DISK"
    fill.data.size = 8.0
    fill.rotation_euler = (0.0, 0.0, 0.0)

    return sun, fill
