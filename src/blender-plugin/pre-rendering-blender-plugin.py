# ________________________________________________________ #

#                            Info                          #
# ________________________________________________________ #

bl_info = {
    "name":         "PreRendering",
    "author":       "Wanja Wischmeier Test",
    "version":      (0, 2),
    "blender":      (2, 80, 0),
    "location":     "Render > PreRender",
    "description":  "Generates a map file from the current scene",
    "warning":      "This is still very experimental, always make sure to save your project first.",
    "doc_url":      "https://github.com/wanjawischmeier/pre-rendering",
    "category":     "Render"
}

# ________________________________________________________ #

#                          Imports                         #
# ________________________________________________________ #

# Blender modules
import bpy
from bpy.types import (
    Operator,
    TOPBAR_MT_render,
    Context,
    Object,
    Scene
)
from bpy.props import (
    FloatProperty,
    StringProperty,
    FloatVectorProperty,
    EnumProperty
)
# Libraries
from numpy import arange, array, empty, ndarray
from math import radians
from json import dumps
from os.path import join

# ________________________________________________________ #

#                      Helper Methods                      #
# ________________________________________________________ #

def toRadians(degrees: list) -> list:
    radians_list = []

    for degree in degrees:
        radians_list.append(radians(degree))

    return radians_list

def getNeeded(start: list, end: list, step_size: float) -> ndarray:
    needed = []
    for x in arange(start[0], end[0] + step_size, step_size):
        for y in arange(start[1], end[1] + step_size, step_size):
            for z in arange(start[2], end[2] + step_size, step_size):
                needed.append([x, y, z])
    return array(needed)

def setKeyframe(self, context: Context, frame: int, location: list, keyframe_type = 'Location') -> None:
    context.scene.frame_set(frame)
    self.location = location
    bpy.ops.anim.keyframe_insert(type=keyframe_type)
Object.setKeyframe = setKeyframe

def setKeyframes(self, context: Context, locations: list) -> None:
    for i in range(len(locations)):
        self.setKeyframe(context, i, locations[i])
Object.setKeyframes = setKeyframes

def setUpForRendering(self, far_clip: int) -> None:
    self.rotation_euler = toRadians([90, 0, 0])
    self.data.type = 'PANO'
    self.data.clip_end = far_clip
    self.data.cycles.panorama_type = 'EQUIRECTANGULAR'
    bpy.ops.anim.keyframe_clear_v3d()
Object.setUpForRendering = setUpForRendering

def setRenderSettings(self, path: str, resolution: tuple, frame_end: int) -> None:
    self.render.engine = 'CYCLES'
    self.render.filepath = path
    self.render.image_settings.file_format = 'PNG'
    self.render.fps = 30
    self.render.resolution_x = resolution[0]
    self.render.resolution_y = resolution[1]
    self.frame_start = 0
    self.frame_end = frame_end
Scene.setRenderSettings = setRenderSettings

def createConfigFile(path: str, resolution: int, fclip: float, mx_width: float, offsets: ndarray):
    with open(join(path, ".mapconfig"), 'w') as file:
        config = dumps({
            "resolution": resolution,
            "fclip": fclip,
            "mx_width": mx_width,
            "offsets": offsets.ravel().tolist()
        })
        file.write(config)

# ________________________________________________________ #

#                        Structures                        #
# ________________________________________________________ #

qualitys = [
    ("low",     "Low quality (720p)",  "Low filesize and fast reading, but may look bad"),
    ("medium",  "Medium quality (1080p)",   "Propably the best option for large maps"),
    ("high",    "High quality (2k)",        "Will result in a large map file, but capture more detail"),
    ("ultra",   "Very high quality (4k)",   "Very large filesize, only for very good PC's")
]

resolutions = {
    "low":      1280,
    "medium":   1920,
    "high":     3840,
    "ultra":    7680
}
resolution_default = resolutions.get("medium")

# ________________________________________________________ #

#                           UI                             #
# ________________________________________________________ #

def add_setup_button(self, context):
    layout = self.layout
    layout.operator(
        TOPBAR_OT_prerender_setup.bl_idname,
        text="Setup map file test")

class TOPBAR_OT_prerender_setup(Operator):
    bl_idname = "render.prerender_setup"
    bl_label = "Setup selected camera for PreRendering"
    bl_space_type = "VIEW3D"
    bl_region_type = "UI"
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = "Setup a fixed area for PreRendering"

    start: FloatVectorProperty(
        name="Start Position",
        subtype='XYZ',
        description="The start of the area to prerender",
    )
    end: FloatVectorProperty(
        name="End Position",
        subtype='XYZ',
        default=(10, 10, 0),
        description="The end of the area to prerender",
    )
    step_size: FloatProperty(
        name="Step Size",
        default=1,
        description="The size of the gap between renders",
    )
    far_clip: FloatProperty(
        name="Far Clip",
        default=10,
        description="",
    )
    quality: EnumProperty(
        name = "Quality",
        items = qualitys,
        default = "medium",
        description = "The quality of the map file, mainly determined by it's resolution"
    )
    path: StringProperty(
        name = "Target Path",
        default = "",
        description = "Where the map file should be saved"
    )

    @classmethod
    def poll(cls, context):
        return context.object != None and context.object.type == 'CAMERA'

    def invoke(self, context, event):
        return context.window_manager.invoke_props_dialog(self)

# ________________________________________________________ #

#                        Main Code                         #
# ________________________________________________________ #

    def execute(self, context):
        frames = getNeeded(self.start, self.end, self.step_size)

        resolution = resolutions.get(self.quality, resolution_default)
        mx_width = max(self.end)

        scene = context.scene
        scene.setRenderSettings(self.path, resolution, len(frames) -1)

        cam = context.object
        cam.setUpForRendering(self.far_clip)
        cam.setKeyframes(context, frames)

        createConfigFile(self.path, resolution, self.far_clip, mx_width, frames)

        return {'FINISHED'}

# ________________________________________________________ #

#                      Registration                        #
# ________________________________________________________ #

def add_object_manual_map():
    url_manual_prefix = "https://github.com/wanjawischmeier/pre-rendering"
    url_manual_mapping = (
        ("bpy.ops.render.prerender", "scene_layout/object/types.html")
    )
    return url_manual_prefix, url_manual_mapping

def register():
    bpy.utils.register_class(TOPBAR_OT_prerender_setup)
    bpy.utils.register_manual_map(add_object_manual_map)
    TOPBAR_MT_render.append(add_setup_button)

def unregister():
    bpy.utils.unregister_class(TOPBAR_OT_prerender_setup)
    bpy.utils.unregister_manual_map(add_object_manual_map)
    TOPBAR_MT_render.remove(add_setup_button)

if __name__ == "__main__":
    register()