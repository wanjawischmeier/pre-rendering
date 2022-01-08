drivers = bpy.data.scenes["Scene"].node_tree.animation_data.drivers

def handler(scene):
    for driver in drivers:
        driver.driver.expression += " "
        driver.driver.expression = driver.driver.expression[:-1]

bpy.app.handlers.frame_change_post.clear()
bpy.app.handlers.frame_change_post.append(handler)