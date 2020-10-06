bl_info = {
    "name": "PreRendering",
    "author": "Wanja Wischmeier",
    "version": (0, 1),
    "blender": (2, 80, 0),
    "location": "Render > PreRender",
    "description": "Generates a map file from the current scene",
    "warning": "This is still very experimental, always make sure to save your project first.",
    "doc_url": "https://sites.google.com/view/prerendering/",
    "category": "Render",
}

cache = {
    "setup": False,
    "camera": None
}

import bpy
from bpy.types import AddonPreferences, Operator, Panel, PropertyGroup

from bpy.props import FloatVectorProperty
from bpy_extras.object_utils import AddObjectHelper, object_data_add

class TOPBAR_OT_prerender_setup(Operator):
    bl_idname = "render.prerender_setup"
    bl_label = "Setup selected camera for PreRendering"
    bl_space_type = "VIEW3D"
    bl_region_type = "UI"
    bl_options = {'REGISTER', 'UNDO'}

    start: FloatVectorProperty(
        name="Start Position",
        subtype='XYZ',
        description="The start of the area to prerender",
    )
    end: FloatVectorProperty(
        name="End Position",
        default=(0.0, 1.0, 1.0),
        subtype='XYZ',
        description="The start of the area to prerender",
    )

    @classmethod
    def poll(cls, context):
        return context.object != None and context.object.type == 'CAMERA'

    def invoke(self, context, event):
        if context.object != None and context.object.type == 'CAMERA':
            return context.window_manager.invoke_props_dialog(self)

    def execute(self, context):
        
        cache["camera"] = context.object
        # bpy.ops.mesh.primitive_torus_add(align='WORLD', location=(0, 0, 0), rotation=(0, 0, 0), major_radius=1, minor_radius=0.25, abso_major_rad=1.25, abso_minor_rad=0.75)
        cache["setup"] = True

        return {'FINISHED'}


class TOPBAR_OT_prerender(Operator):
    bl_idname = "render.prerender"
    bl_label = "PreRender using the setup camera"
    bl_space_type = "VIEW3D"
    bl_region_type = "UI"
    bl_options = {'REGISTER', 'UNDO'}

    @classmethod
    def poll(cls, context):
        return cache["setup"]

    def invoke(self, context, event):
        if cache["setup"]:
            return context.window_manager.invoke_props_dialog(self)

    def execute(self, context):
        scene = bpy.context.scene

        scene.render.image_settings.file_format = 'FFMPEG'
        scene.render.ffmpeg.format = 'MPEG4'
        scene.render.resolution_x = 100
        scene.render.resolution_y = 10

        scene.render.filepath = "F:/image.png"
        
        bpy.ops.render.render(animation = True)

        return {'FINISHED'}


def add_setup_button(self, context):
    print("setup")
    self.layout.operator(
        TOPBAR_OT_prerender_setup.bl_idname,
        text="Setup map file",
        icon='DECORATE_KEYFRAME')

def add_generate_button(self, context):
    print("gener")
    self.layout.operator(
        TOPBAR_OT_prerender.bl_idname,
        text="Generate map file",
        icon='PLUGIN')


def add_object_manual_map():
    url_manual_prefix = "https://sites.google.com/view/prerendering/"
    url_manual_mapping = (
        ("bpy.ops.render.prerender", "scene_layout/object/types.html"),
    )
    return url_manual_prefix, url_manual_mapping


def register():
    bpy.utils.register_class(TOPBAR_OT_prerender_setup)
    bpy.utils.register_class(TOPBAR_OT_prerender)
    bpy.utils.register_manual_map(add_object_manual_map)
    bpy.types.TOPBAR_MT_render.append(add_setup_button)
    bpy.types.TOPBAR_MT_render.append(add_generate_button)
    print("regist")


def unregister():
    bpy.utils.unregister_class(TOPBAR_OT_prerender_setup)
    bpy.utils.unregister_class(TOPBAR_OT_prerender)
    bpy.utils.unregister_manual_map(add_object_manual_map)
    bpy.types.TOPBAR_MT_render.remove(add_setup_button)
    bpy.types.TOPBAR_MT_render.remove(add_generate_button)


if __name__ == "__main__":
    register()
