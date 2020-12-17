bl_info = {
    "name": "PreRender",
    "author": "Wanja Wischmeier",
    "version": (1, 0),
    "blender": (2, 80, 0),
    "location": "View3D > Object",
    "description": "Calculates the camera keyframes for PreRendering",
    "warning": "Please make sure to have your camera set as main camera",
    "wiki_url": "",
    "category": "Object",
}

import bpy
from bpy.types import (
    AddonPreferences, 
    Operator, 
    Panel, 
    PropertyGroup,
)
from bpy.props import (
    IntProperty,
    BoolProperty,
)

class OBJECT_OT_prerender(Operator):
    bl_label = "PreRender"
    bl_idname = "object.prerender"
    bl_description = "Calculates the camera keyframes for PreRendering"
    bl_space_type = "VIEW_3D"
    #bl_region_type = "UI"
    bl_options = {'REGISTER', 'UNDO'}
    
    
    length_x: bpy.props.IntProperty(
        name = "Size: X",
        default = 10,
        min = 1,
        #max = 10000,
        description = "The length of the Camera path on the x axis"
    )
    length_y: bpy.props.IntProperty(
        name = "Size: Y",
        default = 10,
        min = 1,
        #max = 10000,
        description = "The length of the Camera path on the y axis"
    )
    axis: bpy.props.IntProperty(
        name = "EXPERIMENTAL: Axis",
        default = 1,
        min = 1,
        max = 4,
        description = "The ammount of axis for the camera to calculate"
    )
    steps: bpy.props.IntProperty(
        name = "Step size",
        default = 1,
        min = 1,
        max = 1,
        description = "Change the size of each step\nNot supported yet"
    )
    performance_mode: bpy.props.BoolProperty(
        name = "Performance Mode",
        default = True,
        description = "Only calculate needed frames\nThis can save a lot of processing time"
    )
    logging: bpy.props.BoolProperty(
        name = "Logging",
        default = False,
        description = "Enable/Disable logging in the System Console (Window > Toggle System Console)\nLogging will heavily increase calculating time, only useful for development uses"
    )
    
    def invoke(self, context, event):
        return context.window_manager.invoke_props_dialog(self)
    
    def execute(self, context):
        
        def clear():
            print("\n" * 100)

        def getpercent(w, g):
            return (w/g) *1000

        #Settings#
        length_x = self.length_x +1
        length_y = self.length_y +1
        axis = self.axis
        steps = self.steps
        logging = self.logging
        performance_mode = self.performance_mode
        length_total = length_x * length_y * axis *2 -1
        clear()

        camera = bpy.context.scene.camera
        bpy.context.object.location[0] = 0
        bpy.context.object.location[1] = 0

        window_manager = bpy.context.window_manager
        window_manager.progress_begin(0, 1000)

        scene = bpy.data.scenes['Scene']
        bpy.context.scene.frame_start = 0
        bpy.context.scene.frame_end = length_total
        bpy.context.scene.frame_set(0)
        camera.animation_data_clear()

        clear()
        if logging:
            print("Length on x:\t\t" + str(length_x -1))
            print("Length on y:\t\t" + str(length_y -1))
            print("Total length (frames):\t" + str(length_total))
            print("Camera name:\t\t" + camera.name)
        #End#

        #coordinates = { x : 0, y : 0 }
        frame = 0
        keyframe_set = False

        if (axis == 1):
            scene.timeline_markers.new('axis_COUNT=1 | MOVE_TYPE="FORWARDS_X"', frame = frame)
            for y in range(length_y):
                if performance_mode:
                    camera.keyframe_insert(data_path = "location", frame = frame -1)
                    keyframe_set = False
                    if logging:
                        print("Keyframe_id_1 set at (x : " + 
                            str(round(bpy.context.object.location[0])) + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                            ")")
                bpy.context.object.location[1] = -y

                for x in range(length_x):
                    bpy.context.object.location[0] = x
                    if performance_mode and not keyframe_set:
                        camera.keyframe_insert(data_path = "location", frame = frame)
                        keyframe_set = True
                        if logging:
                            print("Keyframe_id_2 set at (x : " + 
                                str(round(bpy.context.object.location[0])) + 
                                "\t| y : " + str(round(bpy.context.object.location[1])) + 
                                ")")
                        
                    if not performance_mode:
                        camera.keyframe_insert(data_path = "location", frame = frame)

                    if logging:
                        print(
                            "(x : " + str(round(bpy.context.object.location[0])) + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                            ")\t|  frame : " + str(frame)
                            )
                    frame += 1




        elif (axis == 2):
            #forwards#
            scene.timeline_markers.new('axis_COUNT=2 | MOVE_TYPE="FORWARDS_X"', frame = frame)
            for y in range(length_y):
                bpy.context.object.location[1] = -y

                for x in range(length_x):
                    bpy.context.object.location[0] = x
                    camera.keyframe_insert(data_path = "location", frame = frame)
                    
                    if logging:
                        print(
                            "(x : " + str(round(bpy.context.object.location[0])) + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                             ")\t|  frame : " + str(frame)
                            )
                    frame += 1
                    window_manager.progress_update(getpercent(frame, length_total))
            
            scene.timeline_markers.new('axis_COUNT=2 | MOVE_TYPE="FORWARDS_Y"', frame = frame)
            for x in range(length_x):
                bpy.context.object.location[0] = x

                for y in range(length_y):
                    bpy.context.object.location[1] = -y  
                    camera.keyframe_insert(data_path = "location", frame = frame)
                    
                    if logging:
                        print(
                            "(x : " + str(round(bpy.context.object.location[0])) + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                            ")\t|  frame : " + str(frame)
                            )
                    frame += 1
                    window_manager.progress_update(getpercent(frame, length_total))
            #backwards#
            length_x -= 1
            length_y -= 1
            y = -length_y
            scene.timeline_markers.new('axis_COUNT=2 | MOVE_TYPE="BACKWARDS_X"', frame = frame)
            while(y <= 0):
                bpy.context.object.location[1] = y
                x = length_x

                while(x >= 0):
                    bpy.context.object.location[0] = x
                    camera.keyframe_insert(data_path = "location", frame = frame)
                    if logging:
                        print(
                            "(x : " + str(round(bpy.context.object.location[0])) + 
                            "(" + str(x) + ")" + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                            "(" + str(y) + ")" + 
                             ")\t|  frame : " + str(frame)
                            )
                    frame += 1
                    window_manager.progress_update(getpercent(frame, length_total))
                    x -= 1
                y += 1

            x = length_x
            scene.timeline_markers.new('axis_COUNT=2 | MOVE_TYPE="BACKWARDS_Y"', frame = frame)
            while(x >= 0):
                bpy.context.object.location[0] = x
                y = -length_y

                while(y <= 0):
                    bpy.context.object.location[1] = y
                    camera.keyframe_insert(data_path = "location", frame = frame)
                    if logging:
                        print(
                            "(x : " + str(round(bpy.context.object.location[0])) + 
                            "(" + str(x) + ")" + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                            "(" + str(y) + ")" + 
                             ")\t|  frame : " + str(frame)
                            )
                    frame += 1
                    window_manager.progress_update(getpercent(frame, length_total))
                    y += 1
                x -= 1




        elif (axis == 3 or axis == 4):
            #forwards#
            scene.timeline_markers.new('axis_COUNT=4 | MOVE_TYPE="FORWARDS_X"', frame = frame)
            for y in range(length_y):
                bpy.context.object.location[1] = -y

                for x in range(length_x):
                    bpy.context.object.location[0] = x
                    camera.keyframe_insert(data_path = "location", frame = frame)
                    
                    if logging:
                        print(
                            "(x : " + str(round(bpy.context.object.location[0])) + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                             ")\t|  frame : " + str(frame)
                            )
                    frame += 1
                    window_manager.progress_update(getpercent(frame, length_total))
            
            scene.timeline_markers.new('axis_COUNT=4 | MOVE_TYPE="FORWARDS_Y"', frame = frame)
            for x in range(length_x):
                bpy.context.object.location[0] = x

                for y in range(length_y):
                    bpy.context.object.location[1] = -y  
                    camera.keyframe_insert(data_path = "location", frame = frame)
                    
                    if logging:
                        print(
                            "(x : " + str(round(bpy.context.object.location[0])) + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                            ")\t|  frame : " + str(frame)
                            )
                    frame += 1
                    window_manager.progress_update(getpercent(frame, length_total))
            #backwards#
            length_x -= 1
            length_y -= 1
            y = -length_y
            scene.timeline_markers.new('axis_COUNT=4 | MOVE_TYPE="BACKWARDS_X"', frame = frame)
            while(y <= 0):
                bpy.context.object.location[1] = y
                x = length_x

                while(x >= 0):
                    bpy.context.object.location[0] = x
                    camera.keyframe_insert(data_path = "location", frame = frame)
                    if logging:
                        print(
                            "(x : " + str(round(bpy.context.object.location[0])) + 
                            "(" + str(x) + ")" + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                            "(" + str(y) + ")" + 
                             ")\t|  frame : " + str(frame)
                            )
                    frame += 1
                    window_manager.progress_update(getpercent(frame, length_total))
                    x -= 1
                y += 1

            x = length_x
            scene.timeline_markers.new('axis_COUNT=4 | MOVE_TYPE="BACKWARDS_Y"', frame = frame)
            while(x >= 0):
                bpy.context.object.location[0] = x
                y = -length_y

                while(y <= 0):
                    bpy.context.object.location[1] = y
                    camera.keyframe_insert(data_path = "location", frame = frame)
                    if logging:
                        print(
                            "(x : " + str(round(bpy.context.object.location[0])) + 
                            "(" + str(x) + ")" + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                            "(" + str(y) + ")" + 
                             ")\t|  frame : " + str(frame)
                            )
                    frame += 1
                    window_manager.progress_update(getpercent(frame, length_total))
                    y += 1
                x -= 1

            start_y = length_y
            scene.timeline_markers.new('axis_COUNT=4 | MOVE_TYPE="DIAGONAL_XY"', frame = frame)
            while(start_y >= 0):
                y = start_y
                x = 0
                bpy.context.object.location[1] = -y

                while(y <= length_y):
                    bpy.context.object.location[0] = x
                    camera.keyframe_insert(data_path = "location", frame = frame)
                    
                    if logging:
                        print(
                            "(x : " + str(round(bpy.context.object.location[0])) + 
                            "\t| y : " + str(round(bpy.context.object.location[1])) + 
                             ")\t|  frame : " + str(frame)
                            )
                    x += 1
                    y += 1
                    frame += 1
                    window_manager.progress_update(getpercent(frame, length_total))
                start_y -= 1




        else:
            print("More than 4 axis not supported")

        fcurves = camera.animation_data.action.fcurves
        for fcurve in fcurves:
            for kf in fcurve.keyframe_points:
                kf.interpolation = 'LINEAR'
                
        clear()
        print("FINISHED")
        return {'FINISHED'}

def menu_func(self, context):
    layout.operator(OBJECT_OT_prerender.bl_idname)
    
def register():
    bpy.utils.register_class(OBJECT_OT_prerender)
    bpy.types.VIEW3D_MT_object.append(menu_func)
    
def unregister():
    bpy.utils.unregister_class(OBJECT_OT_prerender)
    bpy.types.TIMELINE_MT_object.remove(menu_func)

if __name__ == "__main__":
    register()
