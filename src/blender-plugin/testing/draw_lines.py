import bpy
import time
import math
import threading
import random
from math import atan2, asin, sqrt
from mathutils import Vector

texture_name = 'OutlineTexture'
texture_width = 2048
texture_height = 512

# Create a new image (equirectangular projection) if neccessary
if not texture_name in bpy.data.images:
    bpy.ops.image.new(name=texture_name, width=texture_width, height=texture_height, color=(0.0, 0.0, 0.0, 1.0), alpha=True)
line_texture = bpy.data.images[texture_name]

# Set the line color
line_color = (1.0, 1.0, 1.0, 1.0)

def interpolate_points(p0, p1, t):
    return p0 + t * (p1 - p0)

def spherical_coordinates(p: Vector):
    lon = atan2(p.x, p.y)
    lat = asin(p.z / p.length)

    return lon, lat

def uv_coordinates(lon, lat):
    u = (lon / (2 * math.pi)) + 0.5
    v = (lat / math.pi) + 0.5

    return u, v

# Convert UV coordinates to pixel coordinates
def texture_coordinates(u, v):
    x = int(u * texture_width)
    y = int(v * texture_height)

    return x, y

def texture_index(x,u y):
    return (y * texture_width + x) * 4  # 4 channels (RGBA)

def spherical_to_texture_coordinates(lon, lat):
    u, v = uv_coordinates(lon, lat)
    return texture_coordinates(u, v)

def lon_derrivative(p):
    x_sq = p.x ** 2
    y_sq = p.y ** 2

    numerator = x_sq - y_sq
    denominator = (x_sq + y_sq) ** 2

    return numerator / denominator

def lat_derrivative(p):
    z_sq = p.z ** 2
    d_sq = p.length ** 2

    denominator = (d_sq - z_sq) * sqrt(1 - z_sq / d_sq)

    return -1 / denominator

def draw_projected_line(p0, p1, cam, refresh_framerate = 1, max_iterations = 10000):
    refresh_frequency = 1 / refresh_framerate
    lon_step_size = (1 / texture_width) * (2 * math.pi)
    p0 -= cam
    p1 -= cam
    t = 0

    lon0, lat0 = spherical_coordinates(p0)
    lon_end, lat_end = spherical_coordinates(p1)

    if lon0 > lon_end or lat0 > lat_end:
        tmp_lon = lon0
        tmp_lat = lat0
        tmp_p = p0

        lon0 = lon_end
        lat0 = lat_end
        p0 = p1
        
        lon_end = tmp_lon
        lat_end = tmp_lat
        p1 = tmp_p

    base_step_size = 0.001
    step_size = base_step_size
    thickness = 2
    thickness_half = math.ceil(thickness / 2)

    last_refresh = time.time()

    for i in range(max_iterations):
        interpolated_point = interpolate_points(p0, p1, t)
        lon0, lat0 = spherical_coordinates(interpolated_point)
        if lon0 > lon_end or lat0 > lat_end:
            break
        
        interpolated_point1 = interpolate_points(p0, p1, t + base_step_size)
        lon1, lat1 = spherical_coordinates(interpolated_point1)

        diff_lon = lon1 - lon0
        diff_lat = lat1 - lat0
        step_size = base_step_size / (diff_lon / lon_step_size)
        
        slope = diff_lon / diff_lat
        slope_perpendicular = -1 / slope

        x, y = spherical_to_texture_coordinates(lon0, lat0)

        if thickness == 1:
            index = texture_index(x, y)
            line_texture.pixels[index:index + 4] = line_color
        elif thickness == 2:
            index = texture_index(x, y)
            line_texture.pixels[index:index + 4] = line_color
            index += texture_width * 4
            line_texture.pixels[index:index + 4] = line_color
        else:
            for offset in range(-thickness_half, thickness_half):
                index = texture_index(x + int(offset * slope_perpendicular), y + offset)
                line_texture.pixels[index:index + 4] = line_color
        
        t += step_size

        current_time = time.time()
        if current_time - last_refresh > refresh_frequency:
            print(f'iteration: {i}\tstep_size: {step_size}')
            bpy.ops.wm.redraw_timer(type='DRAW_WIN_SWAP', iterations=1)
            last_refresh = current_time



p0 = Vector((1, 2, 3))
p1 = Vector((-18, 0, -14))
cam = Vector((-10, -10, 0))

draw_projected_line(p1, p0, cam)


class lineRasterizerThread(threading.Thread):
    def __init__(self, texture, p0, p1):
        threading.Thread.__init__(self)
        self.texture = texture
        self.p0 = p0
        self.p1 = p1
        self.start()

    def run(self):
        print (f'Drawing line from {self.p0} to {self.p1}')


def draw_outline():
    # some context-specific ops require a switch to the image editor
    original_context_type = bpy.context.area.type
    bpy.context.area.type = 'IMAGE_EDITOR'

    # Create a new image (equirectangular projection) if neccessary
    if not texture_name in bpy.data.images:
        bpy.ops.image.new(name=texture_name, width=texture_width, height=texture_height, color=(0.0, 0.0, 0.0, 1.0), alpha=True)
    outline_texture = bpy.data.images[texture_name]

    threads: list[lineRasterizerThread] = []
    thread = lineRasterizerThread(outline_texture, p0, p1)
    threads.append(thread)

    for thread in threads:
        thread.join()

    # switch back to the original context
    bpy.context.area.type = original_context_type


"""

# Define a function to interpolate between two points and draw a line
def draw_line(image, p1, p2, color):
    # Calculate UV coordinates for both points
    u1 = (math.atan2(p1[1], p1[0]) / (2 * math.pi)) + 0.5
    v1 = (math.asin(p1[2]) / math.pi) + 0.5
    u2 = (math.atan2(p2[1], p2[0]) / (2 * math.pi)) + 0.5
    v2 = (math.asin(p2[2]) / math.pi) + 0.5

    # Calculate the number of steps for interpolation
    num_steps = int(max(abs(u2 - u1), abs(v2 - v1)) * max(line_texture.size[0], line_texture.size[1]))

    # Interpolate between the two points and set the pixel color
    for step in range(num_steps + 1):
        t = step / num_steps
        u = u1 + t * (u2 - u1)
        v = v1 + t * (v2 - v1)
        x = int(u * line_texture.size[0])
        y = int(v * line_texture.size[1])
        index = (y * line_texture.size[0] + x) * 4  # 4 channels (RGBA)
        image.pixels[index:index + 4] = color

# Draw lines between vertices
for i in range(len(vertices) - 1):
    draw_line(line_texture, vertices[i], vertices[i + 1], line_color)

# Optionally, close the loop by connecting the last vertex to the first vertex
draw_line(line_texture, vertices[-1], vertices[0], line_color)
"""