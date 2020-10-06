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

from .data import (
    qualitys,
    cache,
    resolutions,
    resolution_default
)
from .methods import setRenderSettings


class TOPBAR_OT_prerender(Operator):
    bl_idname = "render.prerender"
    bl_label = "PreRender using the setup camera"
    bl_space_type = "VIEW3D"
    bl_region_type = "UI"
    bl_options = {'REGISTER', 'UNDO'}

    quality: EnumProperty(
        name = "Quality",
        items = qualitys,
        default = "medium",
        description = "The quality of the map file, mainly determined by it's resolution"
    )

    @classmethod
    def poll(cls, context):
        return cache["setup"]

    def invoke(self, context, event):
        if cache["setup"]:
            return context.window_manager.invoke_props_dialog(self)

    def execute(self, context):
        scene = bpy.context.scene

        resolution = resolutions.get(self.quality, resolution_default)
        setRenderSettings(scene, cache["camera"], resolution, 10)
        print(resolution)
        # bpy.ops.render.render(animation = True)
        # bpy.ops.render.play_rendered_anim()

        return {'FINISHED'}

        
def add_generate_button(self, context):
    self.layout.operator(
        TOPBAR_OT_prerender.bl_idname,
        text="Generate map file",
        icon='NONE')


def add_object_manual_map():
    url_manual_prefix = "https://sites.google.com/view/prerendering/"
    url_manual_mapping = (
        ("bpy.ops.render.prerender", "scene_layout/object/types.html"),
    )
    return url_manual_prefix, url_manual_mapping

def register():
    bpy.utils.register_class(TOPBAR_OT_prerender)
    bpy.utils.register_manual_map(add_object_manual_map)
    bpy.types.TOPBAR_MT_render.append(add_generate_button)


def unregister():
    bpy.utils.unregister_class(TOPBAR_OT_prerender)
    bpy.utils.unregister_manual_map(add_object_manual_map)
    bpy.types.TOPBAR_MT_render.remove(add_generate_button)