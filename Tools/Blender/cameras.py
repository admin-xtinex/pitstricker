import math
import bpy
from mathutils import Vector


def _look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def create_cameras(config):
    cameras = []

    bpy.ops.object.camera_add(location=(4.8, -6.4, 3.0))
    gameplay = bpy.context.object
    gameplay.name = "Camera_Gameplay"
    gameplay.data.lens = 48
    gameplay.data.sensor_width = 36
    _look_at(gameplay, (0.0, config.pit_spacing * 0.72, 0.0))
    cameras.append(gameplay)

    bpy.ops.object.camera_add(location=(8.5, config.pit_spacing, 10.5))
    overview = bpy.context.object
    overview.name = "Camera_Overview"
    overview.data.lens = 52
    _look_at(overview, (0.0, config.pit_spacing, 0.0))
    cameras.append(overview)

    bpy.context.scene.camera = gameplay
    return cameras
