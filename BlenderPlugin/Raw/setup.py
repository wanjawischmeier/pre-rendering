import bpy

from bpy.types import (
    AddonPreferences,
    Operator,
    Panel,
    PropertyGroup
)
from bpy.props import (
    FloatVectorProperty,
    EnumProperty
)

from .data import cache


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

        cache["camera"].location = [0, 0, 4]
        cache["camera"].rotation_euler = [0, 0, 0]

        cache["setup"] = True

        return {'FINISHED'}


def add_setup_button(self, context):
    self.layout.operator(
        TOPBAR_OT_prerender_setup.bl_idname,
        text="Setup map file",
        icon='NONE')

def add_object_manual_map():
    url_manual_prefix = "https://sites.google.com/view/prerendering/"
    url_manual_mapping = (
        ("bpy.ops.render.prerender", "scene_layout/object/types.html"),
    )
    return url_manual_prefix, url_manual_mapping

def register():
    bpy.utils.register_class(TOPBAR_OT_prerender_setup)
    bpy.utils.register_manual_map(add_object_manual_map)
    bpy.types.TOPBAR_MT_render.append(add_setup_button)


def unregister():
    bpy.utils.unregister_class(TOPBAR_OT_prerender_setup)
    bpy.utils.unregister_manual_map(add_object_manual_map)
    bpy.types.TOPBAR_MT_render.remove(add_setup_button)
