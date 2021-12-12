# ________________________________________________________ #

#                          Imports                         #
# ________________________________________________________ #

# Blender modules
from numpy.core.shape_base import block
import bpy
from bpy.types import (
    TOPBAR_MT_render,
    Collection,
    Operator,
    Object,
    Scene
)
from bpy.props import (
    IntProperty,
    FloatProperty,
    StringProperty,
    EnumProperty
)

# Libraries
from numpy import arange, array, ndarray
from math import radians
from json import dumps
from os import makedirs
from os.path import join

# ________________________________________________________ #

#                        Structures                        #
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

qualitys = [
    (
        "low",
        "Low quality (720p)",
        "Low filesize and fast reading, but may look bad"
    ),
    (
        "medium",
        "Medium quality (1080p)",
        "Propably the best option for large maps"
    ),
    (
        "high",
        "High quality (1440p)",
        "Will result in a large map file, but capture more detail"
    ),
    (
        "ultra",
        "Very high quality (2160p)",
        "Very large filesize, only for very good PC's"
    )
]

resolutions = {
    "low":      (1280, 720),
    "medium":   (1920, 1080),
    "high":     (2560, 1440),
    "ultra":    (3840, 2160)
}

class Property:
    def __init__(self, target: str, path: str, description: str=None, default=None) -> None:
        self.target = target
        self.path = path
        self.description = description
        self.default = default

properties: dict[str, Property] = {
    "chunkWidth": Property(
        target="Domain",
        path='["chunkWidth"]',
        default=4,
        description="""
How many positions should fit into each chunk row and column.
This setting can later be changed in the domain
(go to "Object Properties" and expand "Custom Properties")"""
    ),
    "chunkColumns": Property(
        target="Domain",
        path='["chunkColumns"]',
        default=5,
        description="""
How many chunks should fit into each domain column.
This setting can later be changed in the domain
(go to "Object Properties" and expand "Custom Properties")""",
    ),
    "chunkRows": Property(
        target="Domain",
        path='["chunkRows"]',
        default=5,
        description="""
How many chunks should fit into each domain row.
This setting can later be changed in the domain
(go to "Object Properties" and expand "Custom Properties")"""
    ),
    "domainLocation": Property(
        target="Domain",
        path="location[{i}]"
    ),
    "domainScale": Property(
        target="Domain",
        path="scale[{i}]"
    ),
    "chunkPosition": Property(
        target="ChunkBounds",
        path="location[{i}]"
    )
}

variables = {
    "chunkIndex": "(frame%chunkSize)",
    "rowSize": "(chunkSize*chunkColumns)",
    "chunkSize": "(chunkWidth**2)",
    "blockWidth": "(domainScale/chunkColumns/chunkWidth)",
    "blockHeight": "(domainScale/chunkRows/chunkWidth)",
    "domainOffset": "(-domainScale/2+domainLocation)"
}

expressions = {
    "ChunkBounds": {
        "location": [
            "(frame-chunkIndex)/chunkSize%chunkColumns*chunkWidth*blockWidth+domainOffset",
            "(frame-frame%rowSize)/rowSize*chunkWidth*blockWidth+domainOffset"
        ],
        "scale": [
            "domainScale/chunkColumns",
            "domainScale/chunkRows"
        ]
    },
    "ChunkPosition": {
        "location": [
            "chunkPosition+chunkIndex%chunkWidth*blockWidth",
            "chunkPosition+(chunkIndex-chunkIndex%chunkWidth)/chunkWidth*blockWidth"
        ],
        "scale": [
            "blockWidth",
            "blockHeight"
        ]
    }
}

default_domain_size = 20

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

def estimatePanoramaResolution(width: int, height: int, fov: int=90) -> tuple:
    return (
        round(width * 360 / fov),
        round(height * 180 / fov)
    )

def instantiatePreviewPlane(
    self, name: str, context, location=(0.5, 0.5, 0),
    display_bounds=True, apply_transform=True, selectable=False
) -> Object:
    bpy.ops.mesh.primitive_plane_add(
        size=1,
        location=location
    )
    if apply_transform:
        bpy.ops.object.transform_apply()
    obj = context.object
    obj.name = name
    obj.hide_select = not selectable
    obj.hide_render = True
    obj.display.show_shadows = False
    if display_bounds:
        obj.display_type = 'BOUNDS'
    
    self.objects.link(obj)
    context.scene.collection.objects.unlink(obj)
    return obj
Collection.instantiatePreviewPlane = instantiatePreviewPlane

def setUpForRendering(self, near_clip: float, far_clip: float) -> None:
    self.rotation_euler = toRadians([90, 0, 0])
    self.data.type = 'PANO'
    self.data.clip_start = near_clip
    self.data.clip_end = far_clip
    self.data.cycles.panorama_type = 'EQUIRECTANGULAR'
    bpy.ops.anim.keyframe_clear_v3d()
Object.setUpForRendering = setUpForRendering

def setRenderSettings(self, path: str, resolution: tuple, frame_end: int) -> None:
    self.render.engine = 'CYCLES'
    self.render.fps = 30
    self.render.resolution_x = resolution[0]
    self.render.resolution_y = resolution[1]
    self.frame_start = 1
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

class Configuration:
    nclip: float
    fclip: float
    chunkWidth: int
    chunkColumns: int
    chunkRows: int
    blockWidth: float
    blockHeight: float
    blocks: int

def createConfigFile(path: str, config: Configuration) -> None:
    try:
        makedirs(path, exist_ok=True)
    except Exception as e:
        raise OSError(f"Failed to create file at {path}")
    with open(join(path, ".mapconfig"), 'w') as file:
        config = dumps(
            config.__dict__,
            indent = 2,
            separators=(',', ': ')
        )
        file.write(config)

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
    
    chunkWidth: IntProperty(
        name="Chunk Width",
        default=properties["chunkWidth"].default,
        description=properties["chunkWidth"].description
    )
    chunkColumns: IntProperty(
        name="Chunk Columns",
        default=properties["chunkColumns"].default,
        description=properties["chunkColumns"].description
    )
    chunkRows: IntProperty(
        name="Chunk Rows",
        default=properties["chunkRows"].default,
        description=properties["chunkRows"].description
    )

    @classmethod
    def poll(cls, context):
        return 'Domain' not in bpy.data.objects
    
    def invoke(self, context, event):
        return context.window_manager.invoke_props_dialog(self)

    def execute(self, context):
        collection = bpy.data.collections.new("PreRendering")
        context.scene.collection.children.link(collection)

        # Add all objects
        obj = collection.instantiatePreviewPlane(
            "Domain",
            context,
            location=(0, 0, 0),
            apply_transform=False,
            selectable=True
        )
        obj.scale = (default_domain_size, default_domain_size, 1)
        collection.instantiatePreviewPlane("ChunkBounds", context)
        collection.instantiatePreviewPlane("ChunkPosition", context, display_bounds=False)

        bpy.ops.object.select_camera()
        bpy.ops.object.constraint_add(type='COPY_LOCATION')
        camera = context.object
        camera.rotation_euler = [90, 0, 0]
        camera.constraints["Copy Location"].target = collection.objects["ChunkPosition"]

        # Set custom properties
        domain = collection.objects["Domain"]
        domain["chunkWidth"] = self.chunkWidth
        domain["chunkColumns"] = self.chunkColumns
        domain["chunkRows"] = self.chunkRows
        domain.update_tag()

        for expression_collection_name in expressions:
            expression_collection = expressions[expression_collection_name]

            for i in range(2):
                # Add driver to location coordinate
                for expression_path in expression_collection:
                    driver = collection.objects[expression_collection_name].driver_add(expression_path, i).driver
                    driver.type = 'SCRIPTED'
                    
                    expression_pair = expression_collection[expression_path]
                    expression = expression_pair[i]
                    for variable in variables:
                        expression = expression.replace(variable, variables[variable])
                    
                    # Add all properties
                    for property_name in properties:
                        if property_name in expression:
                            property = properties[property_name]
                            path = property.path.replace("{i}", str(i))

                            var = driver.variables.new()
                            var.targets[0].id = collection.objects[property.target]
                            var.targets[0].data_path = path
                            var.name = property_name
                    
                    driver.expression = expression
        
        return {'FINISHED'}

def add_setup_button(self, context):
    layout = self.layout
    layout.operator(
        TOPBAR_OT_prerender_setup.bl_idname,
        text="Scene setup")

class TOPBAR_OT_prerender_setup(Operator):
    bl_idname = "render.prerender_setup"
    bl_label = "Setup"
    bl_space_type = "VIEW3D"
    bl_region_type = "UI"
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = "Set up the selected camera for PreRendering inside the domain"

    near_clip: FloatProperty(
        name="Near Clip",
        default=0.1,
        description="The distance of the near clipping plane from the camera. High values may lead to clipping."
    )
    far_clip: FloatProperty(
        name="Far Clip",
        default=10,
        description="The distance of the far clipping plane from the camera. High values may lead to inprecisions."
    )
    quality: EnumProperty(
        name = "Quality",
        items = qualitys,
        default = "medium",
        description = "The quality of the map file, mainly determined by it's resolution. Please don't change the resolution manually after running this setup (You can change the amount of compression)."
    )
    directory: StringProperty(
        name = "Target Path",
        default = "",
        description = "Where the render and the .mapconfig file should be saved.",
        subtype="DIR_PATH"
    )

    @classmethod
    def poll(cls, context):
        return 'Domain' in bpy.data.objects

    def invoke(self, context, event):
        context.window_manager.fileselect_add(self)
        return {'RUNNING_MODAL'}

    def draw(self, context):
        layout = self.layout
        col = layout.column()
        col.label(text="PreRendering scene setup")
 
        row = col.row(heading="Configuration file")
        row.prop(self, "quality")
        col.prop(self, "near_clip")
        col.prop(self, "far_clip")

# ________________________________________________________ #

#                        Main Code                         #
# ________________________________________________________ #

    def execute(self, context):
        collection = bpy.data.collections['PreRendering']
        domain = collection.objects["Domain"]
        config = Configuration()
        
        config.chunkWidth = domain["chunkWidth"]
        config.chunkColumns = domain["chunkColumns"]
        config.chunkRows = domain["chunkRows"]
        domainScale = tuple(domain.scale)

        config.blockWidth = domainScale[0]/config.chunkColumns/config.chunkWidth
        config.blockHeight = domainScale[1]/config.chunkRows/config.chunkWidth
        config.blocks = config.chunkColumns*config.chunkRows*(config.chunkWidth**2)

        screen_resolution = resolutions.get(self.quality)
        resolution = estimatePanoramaResolution(
            screen_resolution[0],
            screen_resolution[1]
        )

        scene = context.scene
        scene.setRenderSettings(self.directory, resolution, config.blocks)

        bpy.ops.object.select_camera()
        cam = context.object
        cam.setUpForRendering(self.near_clip, self.far_clip)

        createConfigFile(self.directory, config)

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