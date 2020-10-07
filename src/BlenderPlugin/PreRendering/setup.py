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

import os
from math import pi, radians
from .data import cache

preview_collections = {}


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
        
        bpy.ops.anim.keyframe_clear_v3d()
        setKeyframe(0, rotation = toRadians([90, 0, 0]))

        width = round(self.end.x - self.start.x)
        height = round(self.end.y - self.start.y)
        z = round((self.start.z + self.end.z) /2)

        bpy.context.scene.frame_start = 0
        bpy.context.scene.frame_end = (width * height) -1

        index = 0

        for x in range(width):
            for y in range(height):
                setKeyframe(index, [x, y, z])
                index += 1

        cache["setup"] = True

        return {'FINISHED'}


def setKeyframe(frame: int, location = [], rotation = []) -> None:
    bpy.context.scene.frame_current = frame

    if not location == []:
        cache["camera"].location = location
        bpy.ops.anim.keyframe_insert_menu(type='Location')

    if not rotation == []:
        cache["camera"].rotation_euler = rotation
        bpy.ops.anim.keyframe_insert_menu(type='Rotation')


def toRadians(degrees: list) -> list:
    radians_list = []

    for degree in degrees:
        radians_list.append(radians(degree))

    return radians_list

def add_setup_button(self, context):
    layout = self.layout
    pcoll = preview_collections["main"]

    row = layout.row()
    l_icon = pcoll["loading"]

    layout.operator(
        TOPBAR_OT_prerender_setup.bl_idname,
        text="Setup map file",
        icon_value=l_icon.icon_id)

def add_object_manual_map():
    url_manual_prefix = "https://sites.google.com/view/prerendering/"
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
