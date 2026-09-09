import bpy
import os

blend = r"C:\Users\Tisan\Documents\pitstricker\Art\Generated\pit_striker_beach.blend"
out = r"C:\Users\Tisan\Documents\pitstricker\Art\Generated\pit_striker_beach_preview.png"

bpy.ops.wm.open_mainfile(filepath=blend)

# Prefer gameplay camera if present
cam = bpy.data.objects.get("Camera_Gameplay") or bpy.data.objects.get("Camera_Overview")
if cam is None:
    for obj in bpy.data.objects:
        if obj.type == "CAMERA":
            cam = obj
            break
if cam:
    bpy.context.scene.camera = cam

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE"
scene.render.resolution_x = 1280
scene.render.resolution_y = 720
scene.render.filepath = out
bpy.ops.render.render(write_still=True)
print("PREVIEW:", out)
