import bpy


def _principled(material):
    material.use_nodes = True
    return material.node_tree.nodes.get("Principled BSDF")


def _tint(color, factor):
    return (
        min(max(color[0] * factor, 0.0), 1.0),
        min(max(color[1] * factor, 0.0), 1.0),
        min(max(color[2] * factor, 0.0), 1.0),
        color[3] if len(color) > 3 else 1.0,
    )


def make_ground_material(name, color, roughness=0.9, bump_strength=0.02):
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    bsdf = nodes.get("Principled BSDF")
    bsdf.inputs["Roughness"].default_value = roughness

    # Broad colour variation prevents the procedural ground from reading as a flat slab.
    broad_noise = nodes.new("ShaderNodeTexNoise")
    broad_noise.name = "Ground_Broad_Variation"
    broad_noise.inputs["Scale"].default_value = 1.65
    broad_noise.inputs["Detail"].default_value = 4.0
    broad_noise.inputs["Roughness"].default_value = 0.72

    ramp = nodes.new("ShaderNodeValToRGB")
    ramp.name = "Ground_Color_Ramp"
    ramp.color_ramp.elements[0].position = 0.28
    ramp.color_ramp.elements[0].color = _tint(color, 0.58)
    ramp.color_ramp.elements[1].position = 0.76
    ramp.color_ramp.elements[1].color = _tint(color, 1.22)

    # Fine grain drives the normal/bump independently from large-scale colour.
    grain_noise = nodes.new("ShaderNodeTexNoise")
    grain_noise.name = "Ground_Fine_Grain"
    grain_noise.inputs["Scale"].default_value = 34.0
    grain_noise.inputs["Detail"].default_value = 3.0
    grain_noise.inputs["Roughness"].default_value = 0.78

    bump = nodes.new("ShaderNodeBump")
    bump.name = "Ground_Bump"
    bump.inputs["Strength"].default_value = min(max(bump_strength * 7.5, 0.06), 0.60)
    bump.inputs["Distance"].default_value = 0.055

    links.new(broad_noise.outputs["Fac"], ramp.inputs["Fac"])
    links.new(ramp.outputs["Color"], bsdf.inputs["Base Color"])
    links.new(grain_noise.outputs["Fac"], bump.inputs["Height"])
    links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return material


def make_glass_marble_material(name, color):
    material = bpy.data.materials.new(name=name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = 0.08
    bsdf.inputs["Metallic"].default_value = 0.0

    if "Transmission Weight" in bsdf.inputs:
        bsdf.inputs["Transmission Weight"].default_value = 0.52
    elif "Transmission" in bsdf.inputs:
        bsdf.inputs["Transmission"].default_value = 0.52

    if "IOR" in bsdf.inputs:
        bsdf.inputs["IOR"].default_value = 1.46
    return material


def make_marker_material(name, color):
    material = bpy.data.materials.new(name=name)
    bsdf = _principled(material)
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Roughness"].default_value = 0.45
    return material
