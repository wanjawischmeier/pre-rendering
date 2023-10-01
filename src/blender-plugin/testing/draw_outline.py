import bpy
import math
from time import time
from math import atan2, asin
from mathutils import Vector

trailing_zeroes_formatter = '{:<010}'

scene_resolution_x = bpy.context.scene.render.resolution_x
scene_resolution_y = bpy.context.scene.render.resolution_y

nclip = bpy.data.cameras[0].clip_start
fclip = bpy.data.cameras[0].clip_end

def get_texture(name, width = scene_resolution_x, height = scene_resolution_y):
    texture = bpy.data.images.get(name)
    if texture != None and texture.size[0] == width and texture.size[1] == height:
        return texture
    else:
        if not texture == None:
            bpy.data.images.remove(texture)
        bpy.ops.image.new(name=name, width=width, height=height, color=(0.0, 0.0, 0.0, 0.0), alpha=True)
        return bpy.data.images[name]
    
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

def texture_coordinates(u, v, width, height):
    x = int(u * width)
    y = int(v * height)

    return x, y

def texture_index(x, y, width):
    return (y * width + x) * 4  # 4 channels (RGBA)

def spherical_to_texture_coordinates(lon, lat, width, height):
    u, v = uv_coordinates(lon, lat)
    return texture_coordinates(u, v, width, height)

def draw_projected_line(texture, p0, p1, cam,
        texture_width = scene_resolution_x, texture_height = scene_resolution_y,
        max_iterations = 10000, base_step_size = 0.005):
    
    step_size = base_step_size
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

    for i in range(max_iterations):
        interpolated_point = interpolate_points(p0, p1, t)
        lon0, lat0 = spherical_coordinates(interpolated_point)
        if lon0 > lon_end or lat0 > lat_end:
            break
        
        interpolated_point1 = interpolate_points(p0, p1, t + base_step_size)
        lon1 = spherical_coordinates(interpolated_point1)[0]

        diff_lon = lon1 - lon0
        if diff_lon == 0:
            step_size = base_step_size
        else:
            step_size = base_step_size / (diff_lon / lon_step_size)
        t += step_size

        normalized_distance = (interpolated_point.length - nclip) / (fclip - nclip)
        color = [normalized_distance] * 4
        x0, y0 = spherical_to_texture_coordinates(lon0, lat0, texture_width, texture_height)

        index = texture_index(x0, y0, texture_width)
        color_old = texture[index]
        if color_old == 0 or color_old > normalized_distance:
            texture[index:index + 4] = color
        
        index += texture_width * 4
        color_old = texture[index]
        if color_old == 0 or color_old > normalized_distance:
            texture[index:index + 4] = color




width, height = (scene_resolution_x, scene_resolution_y)
# width, height = (512, 128)
outline_texture = get_texture('OutlineTexture', width, height)
temporary_texture = [0.0] * len(outline_texture.pixels)

# Get the camera object
camera = bpy.data.objects.get("ChunkPosition")
camera_location = camera.location

# Get the object to process
obj = bpy.context.object

start_time = time()

if obj is not None and obj.type == 'MESH':
    # Access the mesh data
    mesh = obj.data
    last_refresh = time()

    # Iterate over the edges
    for edge in mesh.edges:
        edge_pos_0 = mesh.vertices[edge.vertices[0]].co.copy()
        edge_pos_1 = mesh.vertices[edge.vertices[1]].co.copy()
        direction = camera_location - edge_pos_0

        neighboring_faces_count = 0
        facing_away_count = 0

        # Iterate over the polygons (faces)
        for polygon in mesh.polygons:
            # Check if the edge is part of the current polygon
            if edge.key in polygon.edge_keys:
                neighboring_faces_count += 1

                # Calculate the dot product between the polygon normal and the direction
                dot_product = polygon.normal.dot(direction)

                if dot_product <= 0:
                    facing_away_count += 1

        # seperation only needed if exactly one is facing away
        index_str = f'{mesh.edges.values().index(edge)}/{len(mesh.edges)}'
        if facing_away_count + 1 == neighboring_faces_count:
            print(f'Drawing line for edge\t{index_str}')
            draw_projected_line(
                temporary_texture, edge_pos_0, edge_pos_1, camera_location,
                texture_width=width, texture_height=height,
            )
        else:
            print(f'Skipping edge\t\t{index_str}')

print(f'Render time: {time() - start_time}\nCopying pixels from buffer...')
outline_texture.pixels = temporary_texture

print(f'Total time elapsed: {time() - start_time}')