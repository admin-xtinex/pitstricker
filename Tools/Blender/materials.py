import bpy


def _principled(material):
    material.use_nodes = True
    return material.node_tree.nodes.get("Principled BSDF")


def make_ground_material(name, color, roughness=0.9, bump_strength=0.02):
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = roughness

    noise = nodes.new("ShaderNodeTexNoise")
    noise.inputs["Scale"].default_value = 5.0
    noise.inputs["Detail"].default_value = 5.0
    noise.inputs["Roughness"].default_value = 0.7

    bump = nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = bump_strength
    bump.inputs["Distance"].default_value = 0.12

    links.new(noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def make_glass_marble_material(name, color):
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = 0.12
    bsdf.inputs["Metallic"].default_value = 0.0

    # Blender 4.x uses "Transmission Weight"; older versions use "Transmission".
    if "Transmission Weight" in bsdf.inputs:
        bsdf.inputs["Transmission Weight"].default_value = 0.45
    elif "Transmission" in bsdf.inputs:
        bsdf.inputs["Transmission"].default_value = 0.45

    if "IOR" in bsdf.inputs:
        bsdf.inputs["IOR"].default_value = 1.45
    return material


def make_marker_material(name, color):
    material = bpy.data.materials.new(name=name)
    bsdf = _principled(material)
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = 0.45
    return material
