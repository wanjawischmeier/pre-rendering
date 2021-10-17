# ________________________________________________________ #

#                            Info                          #
# ________________________________________________________ #

bl_info = {
    "name":         "PreRendering Blender Plugin",
    "author":       "Wanja Wischmeier",
    "version":      (1, 1),
    "blender":      (2, 80, 0),
    "location":     "Render > PreRender",
    "description":  "Allows you to set your scene up for PreRendering. This plugin also creates the according .mapconfig file required for loading the render into unity.",
    "warning":      "This is still very experimental, always make sure to save your project first.",
    "doc_url":      "https://github.com/wanjawischmeier/pre-rendering",
    "category":     "Render"
}

# ________________________________________________________ #

#                          Imports                         #
# ________________________________________________________ #

# Blender modules
from os import name, makedirs
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
    BoolProperty,
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
    self.render.fps = 30
    self.render.resolution_x = resolution
    self.render.resolution_y = resolution / 2
    self.frame_start = 0
    self.frame_end = frame_end

    self.use_nodes = True
    tree = self.node_tree

    for node in tree.nodes:
        tree.nodes.remove(node)

    render_node = tree.nodes.new(type='CompositorNodeRLayers')

    out_node = tree.nodes.new(type='CompositorNodeOutputFile')
    out_node.location = 500, 0
    out_node.label = 'Output'
    out_node.base_path = join(path, 'color')

    format = out_node.format
    format.color_mode = 'RGB'
    format.color_depth = '16'

    out_node.file_slots.remove(out_node.inputs[0])
    out_node.file_slots.new('Color')
    out_node.file_slots.new('Map')

    links = tree.links
    links.new(render_node.outputs['Image'], out_node.inputs['Color'])
    links.new(render_node.outputs['Depth'], out_node.inputs['Map'])
Scene.setRenderSettings = setRenderSettings

def createConfigFile(path: str, fclip: float, mx_width: float, offsets: ndarray):
    off_vectors = [
        {
            "x":offsets[i][0],
            "y":offsets[i][2],
            "z":offsets[i][1]
        }
        for i in range(round(len(offsets)))
    ]

    makedirs(path, exist_ok=True)
    with open(join(path, ".mapconfig"), 'w') as file:
        config = dumps({
            "fClip": fclip,
            "mxWidth": mx_width,
            "offsets": off_vectors
        }, indent = 2, separators=(',', ': '))
        file.write(config)

# ________________________________________________________ #

#                        Structures                        #
# ________________________________________________________ #

qualitys = [
    ("low",     "Low quality (1k)",  "Low filesize and fast reading, but may look bad"),
    ("medium",  "Medium quality (2k)",   "Propably the best option for large maps"),
    ("high",    "High quality (4k)",        "Will result in a large map file, but capture more detail"),
    ("ultra",   "Very high quality (8k)",   "Very large filesize, only for very good PC's")
]

resolutions = {
    "low":      1080,
    "medium":   2160,
    "high":     4320,
    "ultra":    8640
}

# ________________________________________________________ #

#                           UI                             #
# ________________________________________________________ #

def add_create_domain_button(self, context):
    layout = self.layout
    layout.operator(TOPBAR_OT_prerender_create_domain.bl_idname)

class TOPBAR_OT_prerender_create_domain(Operator):
    bl_idname = "render.prerender_create_domain"
    bl_label = "Create Domain"
    bl_space_type = "VIEW3D"
    bl_region_type = "UI"
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = "Create a domain to define an area to PreRender"

    @classmethod
    def poll(cls, context):
        return 'PreRendering Domain' not in bpy.data.objects

    def execute(self, context):
        bpy.ops.mesh.primitive_cube_add(scale = [10, 10, 1])
        domain = context.object
        domain.name = 'PreRendering Domain'
        domain.display_type = 'WIRE'
        domain.hide_render = True

        return {'FINISHED'}

def add_setup_button(self, context):
    layout = self.layout
    layout.operator(
        TOPBAR_OT_prerender_setup.bl_idname,
        text="Set up the scene")

class TOPBAR_OT_prerender_setup(Operator):
    bl_idname = "render.prerender_setup"
    bl_label = "PreRendering Setup"
    bl_space_type = "VIEW3D"
    bl_region_type = "UI"
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = "Set up the selected camera for PreRendering inside a fixed area defined by the PreRendering domain"

    step_size: FloatProperty(
        name="Step Size",
        default=1,
        description="The size of the gap between renders"
    )
    far_clip: FloatProperty(
        name="Far Clip",
        default=10,
        description="The distance of the far clipping plane from the camera. High values may lead to imprecisions."
    )
    quality: EnumProperty(
        name = "Quality",
        items = qualitys,
        default = "medium",
        description = "The quality of the map file, mainly determined by it's resolution. Please don't change the resolution manually after running this setup (You can change the amount of compression)."
    )
    path: StringProperty(
        name = "Target Path",
        default = "",
        description = "Where the render and the .mapconfig file should be saved."
    )
    delete_domain: BoolProperty(
        name = "Delete Domain",
        default = False,
        description = "Wether the domain should be deleted after setting up the camera."
    )

    @classmethod
    def poll(cls, context):
        return 'PreRendering Domain' in bpy.data.objects and context.object != None and context.object.type == 'CAMERA'

    def invoke(self, context, event):
        return context.window_manager.invoke_props_dialog(self)

# ________________________________________________________ #

#                        Main Code                         #
# ________________________________________________________ #

    def execute(self, context):
        domain = bpy.data.objects['PreRendering Domain']
        start = domain.matrix_world @ domain.data.vertices[0].co
        end   = domain.matrix_world @ domain.data.vertices[7].co

        frames = getNeeded(start, end, self.step_size)

        resolution = resolutions.get(self.quality)
        
        mx_width = max(
            end[0] - start[0],
            end[1] - start[1],
            end[2] - start[2]
        )

        scene = context.scene
        scene.setRenderSettings(self.path, resolution, len(frames) -1)

        cam = context.object
        cam.setUpForRendering(self.far_clip)
        cam.setKeyframes(context, frames)

        createConfigFile(self.path, self.far_clip, mx_width, frames)

        if self.delete_domain:
            cam.select_set(False)
            domain.select_set(True)
            bpy.ops.object.delete()
            cam.select_set(True)

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
    bpy.utils.register_manual_map(add_object_manual_map)
    bpy.utils.register_class(TOPBAR_OT_prerender_create_domain)
    bpy.utils.register_class(TOPBAR_OT_prerender_setup)
    TOPBAR_MT_render.append(add_create_domain_button)
    TOPBAR_MT_render.append(add_setup_button)

def unregister():
    bpy.utils.unregister_manual_map(add_object_manual_map)
    bpy.utils.unregister_class(TOPBAR_OT_prerender_create_domain)
    bpy.utils.unregister_class(TOPBAR_OT_prerender_setup)
    TOPBAR_MT_render.remove(add_create_domain_button)
    TOPBAR_MT_render.remove(add_setup_button)

if __name__ == "__main__":
    register()