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
def print_progress_bar(iteration, total, prefix='', decimals=1, length=terminal_columns - len(full_progress_bar), fill='█', printEnd='\r'):
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


def spherical_to_texture_coordinates(lon, lat, width, height):
    u, v = uv_coordinates(lon, lat)
    x = int(u * width)
    y = int(v * height)

    return x, y


def point_to_tc(p, width, height):
    lon, lat = spherical_coordinates(p)
    return spherical_to_texture_coordinates(lon, lat, width, height)


def set_pixel(texture, texture_width, texture_height, x, y, color, thickness_2=True):
    index = (x + y * width) * 4     # 4 channels (RGBA)
    color_old = texture[index + 2]
    if color_old == 0 or color_old > color[2]:
        texture[index:index + 4] = color
    
    if not thickness_2:
        return
    
    set_pixel(texture, texture_width, texture_height, x + 1, y, color, thickness_2=False)
    set_pixel(texture, texture_width, texture_height, x, y + 1, color, thickness_2=False)


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
            col = (1, 0, normalized_distance, 1)

        set_pixel(
            texture, texture_width, texture_height,
            coord[0], coord[1], col
        )

        error -= dy
        if error < 0:
            y += ystep
            error += dx


def get_step_diff(x, y, texture_width, texture_height, p0, p1, step_size=0.01, correction_threshold=0.005):
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
    n = p2.cross(d)
    t = p2.cross(n).dot(p0) / n.dot(n)
    p3 = p0 + t * d

    lon1, lat1 = spherical_coordinates(p3)
    diff_lon = lon1 - lon0
    diff_lat = lat1 - lat0

    correcting = abs(diff_lon) + abs(diff_lat) > correction_threshold
    if not correcting:
        p4 = p3 + step_size * d

        lon0, lat0 = spherical_coordinates(p3)
        lon1, lat1 = spherical_coordinates(p4)
        diff_lon = lon1 - lon0
        diff_lat = lat1 - lat0
    # print(u,v,lon0,lat0,p2,p3,p4, correcting)
    return diff_lon, diff_lat, p3, correcting


def draw_projected_line(texture, p0, p1, width, height, max_iterations=10000):
    tc0 = point_to_tc(p0, width, height)
    tc1 = point_to_tc(p1, width, height)
    x0, y0 = tc0
    x1, y1 = tc1

    if max(abs(x1 - x0), abs(y1 - y0)) < 20:
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
            round(x0), round(y0), (1, x_dominant / 2 - correcting, normalized_distance, 1)
        )
        # print(x0, y0, x_dominant, diff_lon, diff_lat, p0, p1)
        if x_dominant:
            slope = diff_lat / diff_lon
            x0 += sign_lon
            y0 += sign_lat * abs(slope)
            
            if not correcting and ((sign_lon == 1 and x0 >= x1) or (sign_lon == -1 and x0 <= x1)):
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


def draw_outline(bm, matrix_world, all_edges=False):
    global drawn_edges_count

    edge_count = len(bm.edges)
    wm.progress_begin(0, edge_count)

    # Iterate over the edges
    for edge in bm.edges:
        if edge.index != 53 and False:
            continue
        edge_pos_0 = matrix_world @ edge.verts[0].co
        edge_pos_1 = matrix_world @ edge.verts[1].co
        direction0 = camera_location - edge_pos_0
        direction1 = camera_location - edge_pos_1
        edge_pos_0 -= camera_location
        edge_pos_1 -= camera_location

        neighboring_faces_count = 0
        facing_away_count = 0

        # iterate over all linked faces (typically 2)
        for face in edge.link_faces:
            neighboring_faces_count += 1
            if not any(face.normal):
                continue

            # Calculate the dot product between the polygon normal and the direction
            dot_product0 = face.normal.dot(direction0)
            dot_product1 = face.normal.dot(direction1)
            if dot_product0 <= 0 or dot_product1 <= 0:
                facing_away_count += 1
                # print(f'face {face.index}: {face.normal} ({dot_product0}, {dot_product1})')
        
        # seperation only needed if exactly one is facing away
        wm.progress_update(edge.index)
        index_str = f'{edge.index}/{edge_count}' # ,({neighboring_faces_count}, {facing_away_count})
        if 0 < facing_away_count < neighboring_faces_count or facing_away_count == 1 - neighboring_faces_count == 0 or all_edges:
            print_progress_bar(
                edge.index, edge_count,
                prefix=f'Drawing Edge \t{index_str}\t'
            )

            draw_projected_line(
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
outline_texture = get_texture('OutlineTexture', width, height)
temporary_texture = [0.0] * len(outline_texture.pixels)

# Get the camera object
camera = bpy.context.scene.camera
camera_location = camera.matrix_world @ camera.location

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

    # load bmesh
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bm.faces.ensure_lookup_table()

    print(f'Drawing outline of {obj.name}')
    rendered_objects_count += 1

    outline_start = time()
    if obj.name=='Wall' or True:
        draw_outline(bm, obj.matrix_world, all_edges=obj.name=='Plane')
    print_progress_bar(
        1, 1,
        prefix=f'Elapsed time: {round(time() - outline_start, 2)}s    \t'
    )

    bm.free()

print(f'Render time: {round(time() - start_time, 5)}s for {drawn_edges_count} edges on {rendered_objects_count} objects')
print('Copying pixels from buffer to target texture...')

outline_texture.pixels = temporary_texture
print(f'Total time elapsed: {round(time() - start_time, 5)}s\n')