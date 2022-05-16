from time import time
from math import *
from shader_emulator.constants import *
from shader_emulator.vectors import * 
from shader_emulator.textures import *


def dispatch(kernel, dimensions: int2, debug_fps=30, log=False) -> None:
    update_intervall = 1000 / float(debug_fps)
    last_update = round(time() * 1000)
    width, height = dimensions.as_tuple
    resolution = int2(dimensions.x -1, dimensions.y - 1)
    total_pixels = width * height

    for y in range(height):
        for x in range(width):
            id = int2(x, height - 1 - y)
            kernel(id, resolution)
            
            current_time = round(time() * 1000)
            if current_time < last_update + update_intervall:
                continue
            last_update = current_time
            
            if log:
                print(f"({x},\t{y})\t| ({x + y * width}\t/ {total_pixels})")

            cancel = show_debug_textures()

        # exit nested loop, sure there's a proper way to do this
            if cancel:
                break
        else:
            continue
        break