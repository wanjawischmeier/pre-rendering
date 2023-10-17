import os
import bpy
import math
import bmesh
import numpy as np
from time import time
from math import sin, cos, atan2, asin
from mathutils import Vector

trailing_zeroes_formatter = '{:<010}'

scene_resolution_x = bpy.context.scene.render.resolution_x
scene_resolution_y = bpy.context.scene.render.resolution_y

terminal_columns = os.get_terminal_size().columns
terminal_columns_partial = round(terminal_columns / 3 * 2)
full_progress_bar = 'Skipping Edge \t|| 100.0%' + ' ' * 11

nclip = bpy.data.cameras[0].clip_start
fclip = bpy.data.cameras[0].clip_end
drawn_edges_count = 0

# taken from: https://stackoverflow.com/a/34325723/13215204
def print_progress_bar (iteration, total, prefix = '', decimals = 1, length = terminal_columns - len(full_progress_bar), fill = '█', printEnd = "\r"):
    """
    Call in a loop to create terminal progress bar
    @params:
        iteration   - Required  : current iteration (Int)
        total       - Required  : total iterations (Int)
        prefix      - Optional  : prefix string (Str)
        suffix      - Optional  : suffix string (Str)
        decimals    - Optional  : positive number of decimals in percent complete (Int)
        length      - Optional  : character length of bar (Int)
        fill        - Optional  : bar fill character (Str)
        printEnd    - Optional  : end character (e.g. "\r", "\r\n") (Str)
    """
    percent = ("{0:." + str(decimals) + "f}").format(100 * (iteration / float(total)))
    filledLength = int(length * iteration // total)
    bar = fill * filledLength + '-' * (length - filledLength)
    print(f'\r{prefix} |{bar}| {percent}%', end = printEnd)
    # print new line on complete
    if iteration == total:
        print('\n')

def get_texture(name, width = scene_resolution_x, height = scene_resolution_y):
    texture = bpy.data.images.get(name)
    if texture != None and texture.size[0] == width and texture.size[1] == height:
        return texture
    else:
        if not texture == None:
            bpy.data.images.remove(texture)
        bpy.ops.image.new(name=name, width=width, height=height, color=(0.0, 0.0, 0.0, 0.0), alpha=True, float=True)
        return bpy.data.images[name]
    
def interpolate_points(p0, p1, t):
    return p0 + t * (p1 - p0)

def spherical_coordinates(p: Vector):
    lon = atan2(p.x, p.y)
    lat = asin(min(1, max(-1, p.z / p.length)))

    return lon, lat

def uv_coordinates(lon, lat):
    u = (lon / (2 * math.pi)) + 0.5
    v = (lat / math.pi) + 0.5

    return u, v

def texture_coordinates(u, v, width, height):
    x = int(u * width)
    y = int(v * height)

    return x, y

def texture_index(x, y, width):
    return (y * width + x) * 4  # 4 channels (RGBA)

def spherical_to_texture_coordinates(lon, lat, width, height):
    u, v = uv_coordinates(lon, lat)
    return texture_coordinates(u, v, width, height)

def point_to_tc(p, width, height):
    lon, lat = spherical_coordinates(p)
    return spherical_to_texture_coordinates(lon, lat, width, height)

def get_position_along_line(p, p0, p1):
    v = p - p0
    w = p1 - p0
    dot_product = v.dot(w)
    length_sq = w.dot(w)    
    return dot_product / length_sq

def get_position_along_line_2(p, p0, d):
    n = p.cross(d)
    return p.cross(n).dot(p0) / n.dot(n)

def set_pixel(texture, texture_width, texture_height, x, y, color, width_2 = False):
    index = texture_index(x, y, texture_width)
    color_old = texture[index + 2]
    if color_old == 0 or color_old > color[2]:
        texture[index:index + 4] = color
    
    if not width_2:
        return
    
    index = (index + texture_width * 4) % (texture_width * texture_height * 4)
    color_old = texture[index + 2]
    if color_old == 0 or color_old > color[2]:
        texture[index:index + 4] = color

# based on: https://saturncloud.io/blog/bresenham-line-algorithm-a-powerful-tool-for-efficient-line-drawing
def bresenham_line(texture, texture_width, texture_height, tc0, tc1, val0=1, val1=1, col=None):
    x0, y0 = tc0
    x1, y1 = tc1

    dx = abs(x1 - x0)
    dy = abs(y1 - y0)
    slope = dy > dx

    if slope:
        x0, y0 = y0, x0
        x1, y1 = y1, x1

    if x0 > x1:
        x0, x1 = x1, x0
        y0, y1 = y1, y0

    dx = abs(x1 - x0)
    dy = abs(y1 - y0)
    error = dx // 2
    y = y0
    ystep = 1 if y0 < y1 else -1
    total = x1 - x0

    for x in range(x0, x1 + 1):
        coord = (y, x) if slope else (x, y)

        if not col:
            if total == 0:
                t = 0.5
            else:
                t = (x1 - x) / (x1 - x0)
            normalized_distance = val0 * (1 - t) + val1 * t
            col = (1, 0, normalized_distance, normalized_distance)

        set_pixel(
            texture, texture_width, texture_height,
            coord[0], coord[1], col
        )

        error -= dy
        if error < 0:
            y += ystep
            error += dx

def draw_projected_line(texture, p0, p1, cam,
        texture_width = scene_resolution_x, texture_height = scene_resolution_y,
        max_iterations = 10000, base_step_size = 0.01, debug = False):

    p0 -= cam
    p1 -= cam
    t = 0

    lon_lat_0 = spherical_coordinates(p0)
    lon_lat_end = spherical_coordinates(p1)
    tc_0 = spherical_to_texture_coordinates(lon_lat_0[0], lon_lat_0[1], texture_width, texture_height)
    tc_end = spherical_to_texture_coordinates(lon_lat_end[0], lon_lat_end[1], texture_width, texture_height)
    x_diff = abs(tc_end[0] - tc_0[0])

    if x_diff < 10:
        normalized_distance0 = (p0.length - nclip) / (fclip - nclip)
        normalized_distance1 = (p1.length - nclip) / (fclip - nclip)

        bresenham_line(
            texture, texture_width, texture_height,
            tc_0, tc_end, normalized_distance0, normalized_distance1
        )
        return
    
    step_size = base_step_size
    lon_step_size = (1 / texture_width) * (2 * math.pi)

    if lon_lat_0[0] > lon_lat_end[0]:
        lon_lat_tmp = lon_lat_0
        tmp_p = p0

        lon_lat_0 = lon_lat_end
        p0 = p1
        
        lon_lat_end = lon_lat_tmp
        p1 = tmp_p

    lon0, lat0 = lon_lat_0
    lon_end = lon_lat_end[0]

    for i in range(max_iterations):
        interpolated_point = interpolate_points(p0, p1, t)
        lon0, lat0 = spherical_coordinates(interpolated_point)
        if lon0 > lon_end:
            break
        
        interpolated_point1 = interpolate_points(p0, p1, t + base_step_size)
        lon1 = spherical_coordinates(interpolated_point1)[0]

        normalized_distance = (interpolated_point.length - nclip) / (fclip - nclip)
        x0, y0 = spherical_to_texture_coordinates(lon0, lat0, texture_width, texture_height)

        difference = lon1 - lon0
        if difference == 0:
            step_size = base_step_size
        else:
            step_size = base_step_size / (difference / lon_step_size)
        t += step_size

        set_pixel(
            texture, texture_width, texture_height,
            x0, y0, (0.0, normalized_distance, normalized_distance, normalized_distance)
        )

        if debug:
            print(f'iteration: {i}\ttc: ({x0}, {y0})')

# optimized for unit sphere
def get_step_diff(x, y, texture_width, texture_height, p0, p1, step_size = 0.01):
    u = x / texture_width
    v = y / texture_height

    lon0 = (u - 0.5) * 2 * math.pi
    lat0 = (v - 0.5) * math.pi
    
    p2 = Vector((
        cos(lat0) * sin(lon0),
        cos(lat0) * cos(lon0),
        sin(lat0)
    ))

    d = p1 - p0
    t = get_position_along_line_2(p2, p0, d)
    p3 = p0 + t * d

    lon1, lat1 = spherical_coordinates(p3)
    diff_lon = lon1 - lon0
    diff_lat = lat1 - lat0

    correcting = abs(diff_lon) + abs(diff_lat) > 0.005
    if not correcting:
        p4 = p3 + step_size * d

        lon1, lat1 = spherical_coordinates(p4)
        diff_lon = lon1 - lon0
        diff_lat = lat1 - lat0

    return diff_lon, diff_lat, p3, correcting

def interpolate_test(texture, p0, p1, width, height, max_iterations=10000):
    tc0 = point_to_tc(p0, width, height)
    tc1 = point_to_tc(p1, width, height)
    x0, y0 = tc0
    x1, y1 = tc1

    if max(abs(x1 - x0), abs(y1 - y0)) < 100:
        normalized_distance0 = (p0.length - nclip) / (fclip - nclip)
        normalized_distance1 = (p1.length - nclip) / (fclip - nclip)
        
        bresenham_line(
            texture, width, height, tc0, tc1,
            normalized_distance0, normalized_distance1
        )
        return
    
    for _ in range(max_iterations):
        diff_lon, diff_lat, p, correcting = get_step_diff(x0, y0, width, height, p0, p1)
        sign_lon = np.sign(diff_lon)
        sign_lat = np.sign(diff_lat)
        
        normalized_distance = (p.length - nclip) / (fclip - nclip)
        x_dominant = abs(diff_lon) > abs(diff_lat)
        set_pixel(
            texture, width, height,
            round(x0), round(y0), (1, x_dominant / 2 - correcting, normalized_distance, normalized_distance)
        )

        if x_dominant:
            slope = diff_lat / diff_lon
            x0 += sign_lon
            y0 += sign_lat * abs(slope)
            
            if (sign_lon == 1 and x0 >= x1) or (sign_lon == -1 and x0 <= x1):
                break
        else:
            slope = diff_lon / diff_lat
            x0 += sign_lon * abs(slope)
            y0 += sign_lat
            
            if not correcting and ((sign_lat == 1 and y0 >= y1) or (sign_lat == -1 and y0 <= y1)):
                break

    x0 = round(x0); y0 = round(y0)
    if 0 < abs(x1 - x0) + abs(y1 - y0) < 10:
        bresenham_line(
            texture, width, height, (x0, y0), tc1,
            col=(1, 1, normalized_distance, normalized_distance)
        )




def interpolate_pos(lon0, lat0, x, y, t, p0, p1, texture_width, texture_height, lon_step_size, lat_step_size, base_step_size = 0.001, debug = False):
    interpolated_point_0 = interpolate_points(p0, p1, t)
    interpolated_point_1 = interpolate_points(p0, p1, t + base_step_size)
    lon_lat_1 = spherical_coordinates(interpolated_point_1)
    lon1, lat1 = lon_lat_1
    diff_lon = lon1 - lon0
    diff_lat = lat1 - lat0
    switching = abs(diff_lat) > abs(diff_lon)

    if debug:
        print(diff_lon, diff_lat, switching)
    
    if switching:
        x, y = y, x
        lon0, lat0 = lat0, lon0
        lon1, lat1 = lat1, lon1
        diff_lon, diff_lat = diff_lat, diff_lon
        lon_step_size, lat_step_size = lat_step_size, lon_step_size
        texture_width, texture_height = texture_height, texture_width
    
    if lon0 > lon1:
        lon0, lon1 = lon1, lon0
        lat0, lat1 = lat1, lat0
        diff_lon *= -1
        diff_lat *= -1
    
    if diff_lon == 0:
        return
            
    slope = diff_lat / diff_lon
    lon_sign = np.sign(diff_lon)
    lat_sign = np.sign(diff_lat)
    lon_interpolated = lon0 + lon_step_size * lon_sign
    lat_interpolated = lat0 + (lon_interpolated - lon0) * slope * lat_sign

    # sqrt needed?
    length_0_1 = math.sqrt((lon1 - lon0) ** 2 + (lat1 - lat0) ** 2)
    length_0_i = math.sqrt((lon_interpolated - lon0) ** 2 + (lat_interpolated - lat0) ** 2)
    scaling_factor = length_0_i / length_0_1
    t += base_step_size * scaling_factor
    x += int(lon_sign)
    if switching:
        v = uv_coordinates(lat0, lon0)[1]
    else:
        v = uv_coordinates(lon0, lat0)[1]
    y = round(v * texture_height)
    print(v, texture_height, y)
            
    if debug and False:
        print(slope, lon0, lat0, lon_interpolated, lat_interpolated, length_0_1, length_0_i, scaling_factor)

    if switching:
        lon_interpolated, lat_interpolated = lat_interpolated, lon_interpolated
        x, y = y, x

    return lon_interpolated, lat_interpolated, x, y, t, interpolated_point_0


def draw_projected_line_3(texture, p0, p1, cam,
        texture_width = scene_resolution_x, texture_height = scene_resolution_y,
        max_iterations = 10000, base_step_size = 0.0001, debug = False):

    lon_step_size = (1 / texture_width) * (2 * math.pi)
    lat_step_size = (1 / texture_height) * math.pi
    t = 0

    p0 -= cam
    p1 -= cam
    
    lon, lat = spherical_coordinates(p0)
    x, y = spherical_to_texture_coordinates(lon, lat, texture_width, texture_height)
    
    for i in range(max_iterations):
        interpolated = interpolate_pos(lon, lat, x, y, t, p0, p1, texture_width, texture_height, lon_step_size, lat_step_size, debug=debug)
        if not interpolated:
            return
        
        lon, lat, x, y, t, p = interpolated
        # x0, y0 = spherical_to_texture_coordinates(lon, lat, width, height)
        x0, y0 = x, y

        normalized_distance = (p.length - nclip) / (fclip - nclip)

        if debug:
            u, v = uv_coordinates(lon, lat)
            print(f'iteration: {i}\tt: {t}\tperc: ({round(u * width, 4)}, {round(v * height, 4)})\ttc: ({x}, {y})')

        set_pixel(
            texture, texture_width, texture_height,
            x0, y0, (0.0, normalized_distance, normalized_distance, normalized_distance)
        )

        if t >= 1:
            return





def draw_projected_line_2(texture, p0, p1, cam,
        texture_width = scene_resolution_x, texture_height = scene_resolution_y,
        max_iterations = 10000, base_step_size = 0.0001, debug = False):

    lon_step_size = (1 / texture_width) * (2 * math.pi)
    lat_step_size = (1 / texture_height) * math.pi
    t = 0

    p0 -= cam
    p1 -= cam
    
    for i in range(max_iterations):
        interpolated_point_0 = interpolate_points(p0, p1, t)
        interpolated_point_1 = interpolate_points(p0, p1, t + base_step_size)
        lon_lat_0 = spherical_coordinates(interpolated_point_0)
        lon_lat_1 = spherical_coordinates(interpolated_point_1)

        lon0, lat0 = lon_lat_0
        lon1, lat1 = lon_lat_1
        diff_lon = lon1 - lon0
        diff_lat = lat1 - lat0
        switching = abs(diff_lat) > abs(diff_lon)

        if debug:
            print(diff_lon, diff_lat, switching)

        if switching:
            lon0, lat0 = lat0, lon0
            lon1, lat1 = lat1, lon1
            diff_lon, diff_lat = diff_lat, diff_lon
            spherical_step_size = lat_step_size
        else:
            spherical_step_size = lon_step_size

        if lon0 > lon1:
            lon0, lon1 = lon1, lon0
            lat0, lat1 = lat1, lat0
            diff_lon *= -1
            diff_lat *= -1

        if diff_lon == 0:
            return
        
        slope = diff_lat / diff_lon
        lon_sign = np.sign(diff_lon)
        lat_sign = np.sign(diff_lat)

        lon_interpolated = lon0 + spherical_step_size * lon_sign
        lat_interpolated = lat0 + (lon_interpolated - lon0) * slope * lat_sign

        # sqrt needed?
        length_0_1 = math.sqrt((lon1 - lon0) ** 2 + (lat1 - lat0) ** 2)
        length_0_i = math.sqrt((lon_interpolated - lon0) ** 2 + (lat_interpolated - lat0) ** 2)

        scaling_factor = length_0_i / length_0_1
        t += base_step_size * scaling_factor
        
        if debug:
            print(slope, lon0, lat0, lon_interpolated, lat_interpolated, length_0_1, length_0_i, scaling_factor)

        normalized_distance = (interpolated_point_0.length - nclip) / (fclip - nclip)
        if switching:
            lon0, lat0 = lat0, lon0
        x0, y0 = spherical_to_texture_coordinates(lon0, lat0, texture_width, texture_height)

        set_pixel(
            texture, texture_width, texture_height,
            x0, y0, (0.0, normalized_distance, normalized_distance, normalized_distance)
        )

        if debug:
            print(f'iteration: {i}\tt: {t}\ttc: ({x0}, {y0})')

        if t >= 1:
            return

def draw_outline(bm):
    global drawn_edges_count

    edge_count = len(bm.edges)
    wm.progress_begin(0, edge_count)

    # Iterate over the edges
    for edge in bm.edges:
        edge_pos_0 = edge.verts[0].co.copy()
        edge_pos_1 = edge.verts[1].co.copy()
        direction = camera_location - edge_pos_0
        edge_pos_0 -= camera_location
        edge_pos_1 -= camera_location

        neighboring_faces_count = 0
        facing_away_count = 0

        # iterate over all linked faces (typically 2)
        for face in edge.link_faces:
            neighboring_faces_count += 1

            # Calculate the dot product between the polygon normal and the direction
            dot_product = face.normal.dot(direction)

            if dot_product <= 0:
                facing_away_count += 1

        # seperation only needed if exactly one is facing away
        wm.progress_update(edge.index)
        index_str = f'{edge.index}/{edge_count}'
        if facing_away_count + 1 == neighboring_faces_count:
            print_progress_bar(
                edge.index, edge_count,
                prefix=f'Drawing Edge \t{index_str}\t'
            )
            """
            draw_projected_line_3(
                temporary_texture, edge_pos_0, edge_pos_1, camera_location,
                texture_width=width, texture_height=height
            )
            """
            interpolate_test(
                temporary_texture, edge_pos_0, edge_pos_1,
                width, height
            )
            
            drawn_edges_count += 1
        else:
            print_progress_bar(
                edge.index, len(bm.edges),
                prefix=f'Skipping Edge \t{index_str}\t'
            )

    wm.progress_end()



width, height = (scene_resolution_x, scene_resolution_y)
# width, height = (512, 128)
outline_texture = get_texture('OutlineTexture', width, height)
temporary_texture = [0.0] * len(outline_texture.pixels)

# Get the camera object
camera = bpy.data.objects.get("ChunkPosition")
camera_location = camera.location
"""
p0 = Vector((-9, 9, 1))
p1 = Vector((1, 9, 1))

p0 = Vector((-7, 8, -3))
p1 = Vector((-4, 8, 4))

# draw_projected_line_2(temporary_texture, p0.copy(), p1.copy(), camera_location, width, height, debug=True)
p0 -= camera_location
p1 -= camera_location

interpolate_test(temporary_texture, p0.copy(), p1.copy(), width, height)

ll0 = spherical_coordinates(p0)
ll1 = spherical_coordinates(p1)

x0, y0 = spherical_to_texture_coordinates(ll0[0], ll0[1], width, height)
x1, y1 = spherical_to_texture_coordinates(ll1[0], ll1[1], width, height)

set_pixel(temporary_texture, width, height, x0, y0, (1, 0, 1, 1))
set_pixel(temporary_texture, width, height, x1, y1, (0, 1, 1, 1))

print(x0, y0, ll0)
print(x1, y1, ll1)
outline_texture.pixels = temporary_texture
"""

previous_context = bpy.context.area.ui_type
bpy.context.area.ui_type = 'VIEW_3D'
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.object.select_all(action='DESELECT')
print('-' * terminal_columns + '\n')

rendered_objects_count = 0
collections = bpy.context.layer_collection.children
wm = bpy.context.window_manager
start_time = time()

for obj in bpy.data.objects:
    if not obj or obj.type != 'MESH' or obj.hide_render:
        if obj.hide_render:
            print(f'Skipping {obj.name}: Object hidden in renders\n')
        continue
    if not any(collection for collection in obj.users_collection if collection.name in collections and not collections[collection.name].exclude):
        print(f'Skipping {obj.name}: Collection excluded from view layer\n')
        continue

    # select object and grab bmesh
    obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(obj.data)
    bm.faces.ensure_lookup_table()

    print(f'Drawing outline of {obj.name}')
    rendered_objects_count += 1

    outline_start = time()
    # draw_outline(obj.data)
    draw_outline(bm)
    print_progress_bar(
        1, 1,
        prefix=f'Elapsed time: {round(time() - outline_start, 2)}s    \t'
    )

    bm.free()
    bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)

print(f'Render time: {round(time() - start_time, 5)}s for {drawn_edges_count} edges on {rendered_objects_count} objects')
print('Copying pixels from buffer to target texture...')
outline_texture.pixels = temporary_texture

print(f'Total time elapsed: {round(time() - start_time, 5)}s\n')

bpy.context.area.ui_type = previous_context