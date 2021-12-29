# ________________________________________________________ #

#                          Imports                         #
# ________________________________________________________ #

# Blender modules
import bpy
from bpy.types import (
    TOPBAR_MT_render,z
    Collection,
    NodeSocket,
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
from math import radians
from json import dumps
from os import makedirs
from os.path import join




# ________________________________________________________ #

#                        Structures                        #
# ________________________________________________________ #

bl_info = {
    'name':         'PreRendering Blender Plugin',
    'author':       'Wanja Wischmeier',
    'version':      (1, 1),
    'blender':      (2, 80, 0),
    'location':     'Render > PreRender',
    'description':  'Allows you to set your scene up for PreRendering. This plugin also creates the according .mapconfig file required for loading the render into unity.',
    'warning':      'This is still very experimental, always make sure to save your project first.',
    'doc_url':      'https://github.com/wanjawischmeier/pre-rendering',
    'category':     'Render'
}

qualitys = [
    (
        'low',
        'Low quality (720p)',
        'Low filesize and fast reading, but may look bad'
    ),
    (
        'medium',
        'Medium quality (1080p)',
        'Propably the best option for large maps'
    ),
    (
        'high',
        'High quality (1440p)',
        'Will result in a large map file, but capture more detail'
    ),
    (
        'ultra',
        'Very high quality (2160p)',
        '''Very large filesize, only for very good PC's'''
    )
]

resolutions = {
    'low':      (1280, 720),
    'medium':   (1920, 1080),
    'high':     (2560, 1440),
    'ultra':    (3840, 2160)
}

class Property:
    def __init__(self, target: str, path: str, description: str=None, default=None) -> None:
        self.target = target
        self.path = path
        self.description = description
        self.default = default

properties: dict[str, Property] = {
    'chunkWidth': Property(
        target='Domain',
        path='["chunkWidth"]',
        default=4,
        description='''
How many positions should fit into each chunk row and column.
This setting can later be changed in the domain
(go to "Object Properties" and expand "Custom Properties")'''
    ),
    'chunkColumns': Property(
        target='Domain',
        path='["chunkColumns"]',
        default=5,
        description='''
How many chunks should fit into each domain column.
This setting can later be changed in the domain
(go to "Object Properties" and expand "Custom Properties")''',
    ),
    'chunkRows': Property(
        target='Domain',
        path='["chunkRows"]',
        default=5,
        description='''
How many chunks should fit into each domain row.
This setting can later be changed in the domain
(go to "Object Properties" and expand "Custom Properties")'''
    ),
    'domainLocation': Property(
        target='Domain',
        path='location[{i}]'
    ),
    'domainScale': Property(
        target='Domain',
        path='scale[{i}]'
    ),
    'chunkPosition': Property(
        target='ChunkBounds',
        path='location[{i}]'
    )
}

# Order is important here!
# (Variables with references to other ones have to be declared beneath those)
variables = {
    'blocks':       'chunkColumns*chunkRows*(chunkWidth**2)',
    'clampedFrame': 'frame%blocks',
    'blockWidth':   'domainScale/chunkColumns/chunkWidth',
    'blockHeight':  'domainScale/chunkRows/chunkWidth',
    'domainOffset': '-domainScale/2+domainLocation',
    'chunkSize':    'chunkWidth**2',
    'chunkIndex':   'clampedFrame%chunkSize',
    'rowSize':      'chunkSize*chunkColumns',
}

expressions = {
    'ChunkBounds': {
        'location': [
            '(clampedFrame-chunkIndex)/chunkSize%chunkColumns*chunkWidth*blockWidth+domainOffset',
            '(clampedFrame-clampedFrame%rowSize)/rowSize*chunkWidth*blockHeight+domainOffset'
        ],
        'scale': [
            'domainScale/chunkColumns',
            'domainScale/chunkRows'
        ]
    },
    'ChunkPosition': {
        'location': [
            'chunkPosition+chunkIndex%chunkWidth*blockWidth',
            'chunkPosition+(chunkIndex-chunkIndex%chunkWidth)/chunkWidth*blockHeight'
        ],
        'scale': [
            'blockWidth',
            'blockHeight'
        ]
    }
}

default_domain_size = 20

class Configuration:
    nclip: float
    fclip: float
    blockWidth: float
    blockHeight: float
    chunkWidth: int
    chunkColumns: int
    chunkRows: int




# ________________________________________________________ #

#                      Helper Methods                      #
# ________________________________________________________ #

def toRadians(degrees: list) -> list:
    radians_list = []

    for degree in degrees:
        radians_list.append(radians(degree))

    return radians_list

def estimatePanoramaResolution(width: int, height: int, fov: int=90) -> tuple:
    return (
        round(width * 360 / fov),
        round(height * 180 / fov)
    )

def instantiatePreviewPlane(
    self, name: str, context, location=(0.5, 0.5, 0),
    display_bounds=True, apply_transform=True, selectable=False, ray_visible=False
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

    obj.visible_camera = (obj.visible_diffuse
    ) = (obj.visible_glossy
    ) = (obj.visible_transmission
    ) = (obj.visible_volume_scatter
    ) = obj.visible_shadow = ray_visible
    
    obj.users_collection[0].objects.unlink(obj)
    self.objects.link(obj)

    return obj
Collection.instantiatePreviewPlane = instantiatePreviewPlane

def addZConstraint(self, target) -> None:
    constraint = self.constraints.new('COPY_LOCATION')
    constraint.target = target
    constraint.use_x = constraint.use_y = False
Object.addZConstraint = addZConstraint

def addDriver(self, vars: dict[Object,dict[str,str]], expression = '', key = 'default_value', driver_type = 'SCRIPTED') -> None:
    driver = self.driver_add(key).driver
    driver.type = driver_type

    for var_target in vars:
        var_info = vars[var_target]
        id_type = var_info['type']
        name = data_path = var_info['name']

        if 'path' in var_info:
            data_path = var_info['path']

        var = driver.variables.new()
        var.targets[0].id_type=id_type
        var.targets[0].id = var.targets[0].id=var_target
        var.targets[0].data_path = data_path
        var.name = name

    if driver_type == 'SCRIPTED':
        driver.expression = expression
NodeSocket.addDriver = addDriver

def addFrameClipDriver(self, scene: Scene) -> None:
    addDriver(self, {
        scene: {
            'type': 'SCENE',
            'path': 'frame_end',
            'name': 'frame_end'
        }
    }, expression='min(floor(frame/frame_end*2),1)')
NodeSocket.addFrameClipDriver = addFrameClipDriver

def setUpForRendering(self, near_clip: float, far_clip: float) -> None:
    self.rotation_euler = toRadians([90, 0, 0])
    self.data.type = 'PANO'
    self.data.clip_start = near_clip
    self.data.clip_end = far_clip
    self.data.cycles.panorama_type = 'EQUIRECTANGULAR'
Object.setUpForRendering = setUpForRendering

def createConfigFile(path: str, config: Configuration) -> None:
    try:
        makedirs(path, exist_ok=True)
    except Exception as e:
        raise OSError(f'Failed to create file at {path}')
    with open(join(path, '.mapconfig'), 'w') as file:
        config = dumps(
            config.__dict__,
            indent = 2,
            separators=(',', ': ')
        )
        file.write(config)




# ________________________________________________________ #

#                      Create Domain                       #
# ________________________________________________________ #

def add_create_domain_button(self, context):
    layout = self.layout
    layout.operator(TOPBAR_OT_prerender_create_domain.bl_idname)

class TOPBAR_OT_prerender_create_domain(Operator):
    bl_idname = 'render.prerender_create_domain'
    bl_label = 'Create Domain'
    bl_space_type = 'VIEW3D'
    bl_region_type = 'UI'
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = 'Create a domain to define an area to PreRender'
    
    chunkWidth: IntProperty(
        name='Chunk Width',
        default=properties['chunkWidth'].default,
        description=properties['chunkWidth'].description
    )
    chunkColumns: IntProperty(
        name='Chunk Columns',
        default=properties['chunkColumns'].default,
        description=properties['chunkColumns'].description
    )
    chunkRows: IntProperty(
        name='Chunk Rows',
        default=properties['chunkRows'].default,
        description=properties['chunkRows'].description
    )

    @classmethod
    def poll(cls, context):
        return (
            'Domain' not in bpy.data.objects and
            context.object != None and
            context.object.type == 'CAMERA'
        )
    
    def invoke(self, context, event):
        wm = context.window_manager
        return wm.invoke_props_dialog(self)
    
    def execute(self, context):
        camera = context.object
        collection = bpy.data.collections.new('PreRendering')
        context.scene.collection.children.link(collection)
        context.scene.frame_start = 0
        context.scene.frame_current = 0

        # ---------------- Add all objects ----------------- #
        # __________________________________________________ #

        domain = collection.instantiatePreviewPlane(
            'Domain',
            context,
            location=(0, 0, 0),
            apply_transform=False,
            selectable=True
        )
        domain.scale = (default_domain_size, default_domain_size, 1)

        obj = collection.instantiatePreviewPlane('ChunkBounds', context)
        obj.addZConstraint(domain)

        obj = collection.instantiatePreviewPlane('ChunkPosition', context, display_bounds=False)
        obj.addZConstraint(domain)

        camera.rotation_euler = [radians(90), 0, 0]
        constraint = camera.constraints.new('COPY_LOCATION')
        constraint.target = collection.objects['ChunkPosition']
        constraint.use_offset = True

        # ------------- Set custom properties -------------- #
        # __________________________________________________ #

        domain['chunkWidth'] = self.chunkWidth
        domain['chunkColumns'] = self.chunkColumns
        domain['chunkRows'] = self.chunkRows
        domain['camera'] = camera
        domain.update_tag()

        # ------------------ Add drivers ------------------- #
        # __________________________________________________ #

        for expression_collection_name in expressions:
            expression_collection = expressions[expression_collection_name]

            for i in range(2):
                # Add driver to location coordinate
                for expression_path in expression_collection:
                    driver = collection.objects[expression_collection_name].driver_add(expression_path, i).driver
                    driver.type = 'SCRIPTED'
                    
                    expression_pair = expression_collection[expression_path]
                    expression = expression_pair[i]
                    for variable in reversed(variables):
                        expression = expression.replace(variable, f'({variables[variable]})')
                    
                    # Add all properties
                    for property_name in properties:
                        if property_name in expression:
                            property = properties[property_name]
                            path = property.path.replace('{i}', str(i))

                            var = driver.variables.new()
                            var.targets[0].id = collection.objects[property.target]
                            var.targets[0].data_path = path
                            var.name = property_name
                    
                    driver.expression = expression
        
        # __________________________________________________ #
        
        return {'FINISHED'}




# ________________________________________________________ #

#                       Scene Setup                        #
# ________________________________________________________ #

def add_setup_button(self, context):
    layout = self.layout
    layout.operator(
        TOPBAR_OT_prerender_setup.bl_idname,
        text='Scene setup')

class TOPBAR_OT_prerender_setup(Operator):
    bl_idname = 'render.prerender_setup'
    bl_label = 'Setup'
    bl_space_type = 'VIEW3D'
    bl_region_type = 'UI'
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = 'Set up the selected camera for PreRendering inside the domain'

    near_clip: FloatProperty(
        name='Near Clip',
        default=0.1,
        description='''
The distance of the near clipping plane from the camera. High values may lead to clipping.'''
    )
    far_clip: FloatProperty(
        name='Far Clip',
        default=10,
        description='''
The distance of the far clipping plane from the camera. High values may lead to inprecisions.'''
    )
    quality: EnumProperty(
        name = 'Quality',
        items = qualitys,
        default = 'medium',
        description = '''
The quality of the map file, mainly determined by it's resolution.
Please don't change the resolution manually after running this setup (You can change the amount of compression).'''
    )
    directory: StringProperty(
        name = 'Target Path',
        default = '',
        description = '''
Where the render and the .mapconfig file should be saved.''',
        subtype='DIR_PATH'
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
        col.label(text='PreRendering scene setup')
 
        row = col.row(heading='Configuration file')
        row.prop(self, 'quality')
        col.prop(self, 'near_clip')
        col.prop(self, 'far_clip')

    def execute(self, context):

        # ----------------- Get Informations --------------- #
        # __________________________________________________ #
        
        collection = bpy.data.collections['PreRendering']
        domain = collection.objects['Domain']
        config = Configuration()
        
        config.chunkWidth = domain['chunkWidth']
        config.chunkColumns = domain['chunkColumns']
        config.chunkRows = domain['chunkRows']
        domainScale = tuple(domain.scale)

        config.blockWidth = domainScale[0]/config.chunkColumns/config.chunkWidth
        config.blockHeight = domainScale[1]/config.chunkRows/config.chunkWidth
        blocks = config.chunkColumns*config.chunkRows*(config.chunkWidth**2)

        screen_resolution = resolutions.get(self.quality)
        resolution = estimatePanoramaResolution(
            screen_resolution[0],
            screen_resolution[1]
        )
        
        # --------------- Set Render Settings -------------- #
        # __________________________________________________ #

        scene = context.scene
        scene.render.engine = 'CYCLES'
        scene.render.fps = 30
        scene.render.filepath = self.directory
        scene.render.resolution_x = resolution[0]
        scene.render.resolution_y = resolution[1]
        scene.frame_end = blocks*2 -1
        scene.view_layers[0].use_pass_z = True

        scene.render.image_settings.file_format = 'FFMPEG'
        ffmpeg = scene.render.ffmpeg
        ffmpeg.format = 'MPEG4'
        ffmpeg.ffmpeg_preset='REALTIME'

        # --------------- Set up world shader -------------- #
        # __________________________________________________ #

        tree = bpy.data.worlds['World'].node_tree
        node_output = tree.nodes['World Output']
        use_surface = len(node_output.inputs['Surface'].links) > 0
        use_volume = len(node_output.inputs['Volume'].links) > 0

        # Get references
        if use_surface:
            surface_link = node_output.inputs['Surface'].links[0]
            node_surface = surface_link.from_node
        if use_volume:
            volume_link = node_output.inputs['Volume'].links[0]
            node_volume = volume_link.from_node

        # Create nodes
        if use_surface or use_volume:
            node_value =            tree.nodes.new('ShaderNodeValue')
            node_emission =         tree.nodes.new('ShaderNodeEmission')
            node_value.outputs['Value'].addFrameClipDriver(context.scene)
            node_emission.inputs['Color'].default_value = (0,0,0,1)
        if use_surface:
            node_s_mix_shader = tree.nodes.new('ShaderNodeMixShader')
        if use_volume:
            node_v_mix_shader = tree.nodes.new('ShaderNodeMixShader')
        
        # Position nodes
        if use_surface or use_volume:
            node_output.location = (400, 0)
            node_value.location = (-100, 0)
            node_emission.location = (-100, -100)
        if use_surface:
            node_surface.location = (-400, 150)
            node_s_mix_shader.location = (200, 40)
        if use_volume:
            node_volume.location = (-400, -300)
            node_v_mix_shader.location = (200, -100)

        # Link nodes
        if use_surface:
            tree.links.remove(surface_link)
            tree.links.new(node_value.outputs['Value'], node_s_mix_shader.inputs['Fac'])
            tree.links.new(node_surface.outputs[0], node_s_mix_shader.inputs[1])
            tree.links.new(node_emission.outputs[0], node_s_mix_shader.inputs[2])
            tree.links.new(node_s_mix_shader.outputs[0], node_output.inputs['Surface'])
        if use_volume:
            tree.links.remove(volume_link)
            tree.links.new(node_value.outputs['Value'], node_v_mix_shader.inputs['Fac'])
            tree.links.new(node_volume.outputs[0], node_v_mix_shader.inputs[1])
            tree.links.new(node_emission.outputs[0], node_v_mix_shader.inputs[2])
            tree.links.new(node_v_mix_shader.outputs[0], node_output.inputs['Volume'])


        # -------- Create surface shader node group -------- #
        # __________________________________________________ #

        group = bpy.data.node_groups.new('PreRendering Surface', 'ShaderNodeTree')

        node_input =        group.nodes.new('NodeGroupInput')
        node_output =       group.nodes.new('NodeGroupOutput')
        node_value =        group.nodes.new('ShaderNodeValue')
        node_comb_rgb =     group.nodes.new('ShaderNodeCombineRGB')
        node_emission =     group.nodes.new('ShaderNodeEmission')
        node_mix_shader =   group.nodes.new('ShaderNodeMixShader')
        
        node_value.outputs['Value'].addFrameClipDriver(scene)

        node_input.location[0] =        -400
        node_output.location[0] =       400
        node_mix_shader.location[0] =   200
        node_value.location[1] =        50
        node_emission.location[1] =     -100
        node_comb_rgb.location =        (-200, -100)

        group.inputs.new('NodeSocketShader', 'InputShader')
        group.inputs.new('NodeSocketFloat', 'Roughness')
        group.inputs.new('NodeSocketFloat', 'Alpha')

        group.outputs.new('NodeSocketShader', 'InputShader')
        
        group.inputs['Alpha'].default_value = 1

        group.links.new(node_input.outputs['InputShader'],  node_mix_shader.inputs[1])
        group.links.new(node_input.outputs['Roughness'],    node_comb_rgb.inputs['R'])
        group.links.new(node_input.outputs['Alpha'],        node_comb_rgb.inputs['G'])
        group.links.new(node_comb_rgb.outputs['Image'],     node_emission.inputs['Color'])
        group.links.new(node_value.outputs['Value'],        node_mix_shader.inputs['Fac'])
        group.links.new(node_emission.outputs['Emission'],  node_mix_shader.inputs[2])
        group.links.new(node_mix_shader.outputs['Shader'],  node_output.inputs['InputShader'])

        # ------------ Set up compositor nodes ------------- #
        # __________________________________________________ #

        scene.use_nodes = True
        tree = scene.node_tree

        for node in tree.nodes:
            tree.nodes.remove(node)

        node_r_layers =     tree.nodes.new('CompositorNodeRLayers')
        node_composite =    tree.nodes.new('CompositorNodeComposite')
        node_value =        tree.nodes.new('CompositorNodeValue')
        node_map_range =    tree.nodes.new('CompositorNodeMapRange')
        node_sep_rgba =     tree.nodes.new('CompositorNodeSepRGBA')
        node_comb_rgba =    tree.nodes.new('CompositorNodeCombRGBA')
        node_s_value =      tree.nodes.new('CompositorNodeMath')
        node_m_color =      tree.nodes.new('CompositorNodeMath')
        node_m_depth =      tree.nodes.new('CompositorNodeMath')
        node_a_col_depth =  tree.nodes.new('CompositorNodeMath')
        
        cam = domain['camera']
        cam_data = bpy.data.cameras[cam.name]
        node_value.outputs['Value'].addFrameClipDriver(context.scene)
        node_map_range.inputs['From Min'].addDriver({
            cam_data: {
                'type': 'CAMERA',
                'name': 'clip_start'
            }
        }, driver_type='AVERAGE')
        node_map_range.inputs['From Max'].addDriver({
            cam_data: {
                'type': 'CAMERA',
                'name': 'clip_end'
            }
        }, driver_type='AVERAGE')

        node_s_value.operation = 'SUBTRACT'
        node_m_color.operation = node_m_depth.operation = 'MULTIPLY'
        node_a_col_depth.operation = 'ADD'
        
        node_s_value.inputs[0].default_value = 1

        node_m_color.hide = node_m_depth.hide = node_a_col_depth.hide = True

        node_r_layers.location[0] =     -650
        node_sep_rgba.location[0] =     -150
        node_comb_rgba.location[0] =    450
        node_composite.location[0] =    650
        node_value.location =           (-350, -300)
        node_map_range.location =       (-350, -400)
        node_s_value.location =         (-150, -150)
        node_m_color.location =         (50, -150)
        node_m_depth.location =         (50, -330)
        node_a_col_depth.location =     (250, -240)

        tree.links.new(node_r_layers.outputs['Image'],      node_sep_rgba.inputs['Image'])
        tree.links.new(node_r_layers.outputs['Depth'],      node_map_range.inputs['Value'])
        tree.links.new(node_sep_rgba.outputs['R'],          node_comb_rgba.inputs['R'])
        tree.links.new(node_sep_rgba.outputs['G'],          node_comb_rgba.inputs['G'])
        tree.links.new(node_a_col_depth.outputs['Value'],   node_comb_rgba.inputs['B'])
        tree.links.new(node_sep_rgba.outputs['B'],          node_m_color.inputs[0])
        tree.links.new(node_comb_rgba.outputs['Image'],     node_composite.inputs['Image'])
        tree.links.new(node_value.outputs['Value'],         node_s_value.inputs[1])
        tree.links.new(node_value.outputs['Value'],         node_m_depth.inputs[0])
        tree.links.new(node_s_value.outputs['Value'],       node_m_color.inputs[1])
        tree.links.new(node_map_range.outputs['Value'],     node_m_depth.inputs[1])
        tree.links.new(node_m_color.outputs['Value'],       node_a_col_depth.inputs[0])
        tree.links.new(node_m_depth.outputs['Value'],       node_a_col_depth.inputs[1])

        # -------- Set up the camera for rendering --------- #
        # __________________________________________________ #

        cam.rotation_euler = toRadians([90, 0, 0])
        cam.data.type = 'PANO'
        cam.data.clip_start = self.near_clip
        cam.data.clip_end = self.far_clip
        cam.data.cycles.panorama_type = 'EQUIRECTANGULAR'

        # ----------- Create configuration file ------------ #
        # __________________________________________________ #

        try:
            makedirs(self.directory, exist_ok=True)
        except Exception as e:
            raise OSError(f'Failed to create file at {self.directory}')
        
        with open(join(self.directory, '.mapconfig'), 'w') as file:
            config = dumps(
                config.__dict__,
                indent = 2,
                separators=(',', ': ')
            )
            file.write(config)
        
        # __________________________________________________ #

        return {'FINISHED'}

# ________________________________________________________ #

#                      Registration                        #
# ________________________________________________________ #

def add_object_manual_map():
    url_manual_prefix = 'https://github.com/wanjawischmeier/pre-rendering'
    url_manual_mapping = (
        ('bpy.ops.render.prerender', 'scene_layout/object/types.html')
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

if __name__ == '__main__':
    register()