import bpy
from tempfile import gettempdir as temp

from bpy.types import (
    AddonPreferences,
    Operator,
    Panel,
    PropertyGroup
)
from bpy.props import (
    FloatVectorProperty,
    EnumProperty,
    StringProperty
)

from .data import (
    qualitys,
    cache,
    resolutions,
    resolution_default
)

import os
from .methods import setRenderSettings

preview_collections = {}


class TOPBAR_OT_prerender(Operator):
    bl_idname = "render.prerender"
    bl_label = "PreRender using the setup camera"
    bl_space_type = "VIEW3D"
    bl_region_type = "UI"
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = "PreRender the setup area"

    path: StringProperty(
        name = "Target Path",
        default = temp() + "\\Map.prm",
        description = "Where the map file should be saved"
    )

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

        # bpy.ops.render.render(animation = True)
        # bpy.ops.render.play_rendered_anim()

        return {'FINISHED'}

        
def add_generate_button(self, context):
    layout = self.layout
    pcoll = preview_collections["main"]

    row = layout.row()
    l_icon = pcoll["prerendering"]

    layout.operator(
        TOPBAR_OT_prerender.bl_idname,
        text="Generate map file",
        icon_value=l_icon.icon_id)


def add_object_manual_map():
    url_manual_prefix = "https://github.com/wanjawischmeier/pre-rendering"
    url_manual_mapping = (
        ("bpy.ops.render.prerender", "scene_layout/object/types.html"),
    )
    return url_manual_prefix, url_manual_mapping

def register():
    import bpy.utils.previews
    pcoll = bpy.utils.previews.new()
 
    icons_dir = os.path.join(os.path.dirname(__file__), "icons")
    pcoll.load("prerendering", os.path.join(icons_dir, "prerendering.png"), 'IMAGE')

    preview_collections["main"] = pcoll

    bpy.utils.register_class(TOPBAR_OT_prerender)
    bpy.utils.register_manual_map(add_object_manual_map)
    bpy.types.TOPBAR_MT_render.append(add_generate_button)


def unregister():
    bpy.utils.unregister_class(TOPBAR_OT_prerender)
    bpy.utils.unregister_manual_map(add_object_manual_map)
    bpy.types.TOPBAR_MT_render.remove(add_generate_button)