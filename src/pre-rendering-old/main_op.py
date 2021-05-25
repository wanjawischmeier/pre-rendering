import bpy

class Main_OT_Operator(bpy.types.Operator):
    bl_idname = "view3d.cursor_center"
    bl_label = "Calculate PreRender-Paths"
    bl_description = "Calculate Motion Paths for PreRendering your scene"

    def execute(self, context):
        bpy.ops.view3d.snap_cursor_to_center()

        return {"PreRender-Paths calculated"}
