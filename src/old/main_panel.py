import bpy

class Main_PT_Panel(bpy.types.Panel):
    bl_idname = "Main_PT_Panel"
    bl_label = "PreRendering"
    bl_category = "PreRendering Addon"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"

    def draw(self, context):
        layout = self.layout
        
        row = layout.row()
        row.operator("view3d.cursor_center", text = "Calculate PreRender-Paths")