import bpy
import numpy as np

def on_async(context):
    images = context["images"] # A list of 4 node input images
    inputs = context["inputs"] # A list of 8 node input values
    output = context["outputs"][0] # A list of 1 node output image. Might have more in the future
    
    depth = inputs[0]
    roughness = inputs[1]
    transparency = inputs[2]
    
    output[:] = images[0]