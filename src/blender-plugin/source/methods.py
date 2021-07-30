import bpy
from math import radians

def toRadians(degrees: list) -> list:
    radians_list = []

    for degree in degrees:
        radians_list.append(radians(degree))

    return radians_list
"""
def roundList(input: list) -> list:
    output = []
    for element in input:
        output.append(round(input))
    return output
    # bpy.ops.action.interpolation_type(type='LINEAR')
"""
def setRenderSettings(scene: object, camera: object, resolution: tuple, frame_end: int, far_clip: int) -> None:
    scene.render.engine = 'CYCLES'
    camera.rotation_euler = toRadians([90, 0, 0])
    camera.data.type = 'PANO'
    camera.data.clip_end = far_clip
    camera.data.cycles.panorama_type = 'EQUIRECTANGULAR'
    scene.render.filepath = "C://tmp/map"
    scene.render.image_settings.file_format = 'FFMPEG'
    scene.render.ffmpeg.format = 'MPEG4'
    scene.render.fps = 30
    scene.render.resolution_x = resolution[0]
    scene.render.resolution_y = resolution[1]
    scene.frame_start = 0
    scene.frame_end = frame_end

def setLoc(object, location: list) -> None:
    object.location = location

def setKeyframe(object, frame: int, location: list) -> None:
    print(f"Setting frame2 {str(frame)} to {str(location)}")
    bpy.context.scene.frame_current = frame
    object.location = location
    print(f"Location: {str(object.location)} | target: {str(location)}")
    bpy.ops.anim.keyframe_insert(type='Location')
"""
def setKeyframeOld(frame: int, location = [], rotation = []) -> None:
    bpy.context.scene.frame_current = frame

    if not location == []:
        cache["camera"].location = location
        bpy.ops.anim.keyframe_insert_menu(type='Location')

    if not rotation == []:
        cache["camera"].rotation_euler = rotation
        bpy.ops.anim.keyframe_insert_menu(type='Rotation')
"""
def setKeyframes(object, locations: list) -> None:
    for i in range(len(locations)):
        setKeyframe(object, i, locations[i])

def getNeeded(start: list, end: list, step_size: float) -> list:
    needed = []
    for x in range(start[0], end[0], step_size):
        for y in range(start[1], end[1], step_size):
            for z in range(start[2], end[2], step_size):
                needed.append([x, y, z])
    return needed