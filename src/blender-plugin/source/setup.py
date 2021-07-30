import bpy

from bpy.types import (
    AddonPreferences,
    Operator,
    Panel,
    PropertyGroup
)
from bpy.props import (
    IntProperty,
    IntVectorProperty,
    EnumProperty
)

import os
from .data import *
from .methods import setRenderSettings, setKeyframes, getNeeded, setLoc

preview_collections = {}


class TOPBAR_OT_prerender_setup(Operator):
    bl_idname = "render.prerender_setup"
    bl_label = "Setup selected camera for PreRendering"
    bl_space_type = "VIEW3D"
    bl_region_type = "UI"
    bl_options = {'REGISTER', 'UNDO'}
    bl_description = "Setup a fixed area for PreRendering"

    start: IntVectorProperty(
        name="Start Position",
        subtype='XYZ',
        description="The start of the area to prerender",
    )
    end: IntVectorProperty(
        name="End Position",
        subtype='XYZ',
        default=(10, 10, 1),
        description="The end of the area to prerender",
    )
    step_size: IntProperty(
        name="Step Size",
        default=1,
        description="The size of the gap between renders",
    )
    far_clip: IntProperty(
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

    @classmethod
    def poll(cls, context):
        return context.object != None and context.object.type == 'CAMERA'

    def invoke(self, context, event):
        if context.object != None and context.object.type == 'CAMERA':
            return context.window_manager.invoke_props_dialog(self)

    def execute(self, context):
        # cache["camera"] = context.object
        # start = (round(self.start.x), round(self.start.y), round(self.start.z))
        # end = (round(self.end.x), round(self.end.y), round(self.end.z))
        camera = context.object
        
        # rStart = roundList(self.start)
        # rEnd = roundList(self.end)

        positions = getNeeded(self.start, self.end, self.step_size)
        print(positions)
        bpy.ops.anim.keyframe_clear_v3d()
        resolution = resolutions.get(self.quality, resolution_default)
        setRenderSettings(bpy.context.scene, camera, resolution, len(positions) -1, self.far_clip)

        setKeyframes(camera, positions)
        camera.location = [1, 2, 3]
        setLoc(camera, [2, 3, 4])
        # cache["setup"] = True
        return {'FINISHED'}


def add_setup_button(self, context):
    layout = self.layout
    pcoll = preview_collections["main"]

    row = layout.row()
    l_icon = pcoll["loading"]

    layout.operator(
        TOPBAR_OT_prerender_setup.bl_idname,
        text="Setup map file test",
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
    pcoll.load("loading", os.path.join(icons_dir, "loading.png"), 'IMAGE')

    preview_collections["main"] = pcoll

    bpy.utils.register_class(TOPBAR_OT_prerender_setup)
    bpy.utils.register_manual_map(add_object_manual_map)
    bpy.types.TOPBAR_MT_render.append(add_setup_button)


def unregister():
    bpy.utils.unregister_class(TOPBAR_OT_prerender_setup)
    bpy.utils.unregister_manual_map(add_object_manual_map)
    bpy.types.TOPBAR_MT_render.remove(add_setup_button)
