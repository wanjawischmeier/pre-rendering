import bpy
import math

sphere_name = 'ProjectionSphere'
texture_name = 'LineTexture'

# Assuming you have a list of vertices in world coordinates
vertices = [(1.0, 0.0, 0.0), (0.0, 1.0, 0.0), (-1.0, 0.0, 0.0)]

# Create a UV Sphere if neccessary
if not sphere_name in bpy.data.objects:
    bpy.ops.mesh.primitive_uv_sphere_add(radius=1, location=(0, 0, 0))
    sphere = bpy.context.object
    sphere.select_set(True)
    bpy.context.view_layer.objects.active = sphere
else:
    sphere = bpy.data.objects[sphere_name]

# Create a new image (equirectangular projection) if neccessary
if not texture_name in bpy.data.images:
    bpy.ops.image.new(name=texture_name, width=4096, height=2048, color=(0.0, 0.0, 0.0, 1.0), alpha=True)
line_texture = bpy.data.images[texture_name]

# Set the image as the active UV map for the sphere
sphere.data.uv_textures.active = sphere.data.uv_textures[-1]
sphere.data.uv_textures.active.data[0].image = line_texture

# Set the line color
line_color = (1.0, 1.0, 1.0, 1.0)

# Define a function to interpolate between two points and draw a line
def draw_line(image, p1, p2, color):
    # Calculate UV coordinates for both points
    u1 = (math.atan2(p1[1], p1[0]) / (2 * math.pi)) + 0.5
    v1 = (math.asin(p1[2]) / math.pi) + 0.5
    u2 = (math.atan2(p2[1], p2[0]) / (2 * math.pi)) + 0.5
    v2 = (math.asin(p2[2]) / math.pi) + 0.5

    # Calculate the number of steps for interpolation
    num_steps = int(max(abs(u2 - u1), abs(v2 - v1)) * max(line_texture.width, line_texture.height))

    # Interpolate between the two points and set the pixel color
    for step in range(num_steps + 1):
        t = step / num_steps
        u = u1 + t * (u2 - u1)
        v = v1 + t * (v2 - v1)
        x = int(u * line_texture.width)
        y = int(v * line_texture.height)
        index = (y * line_texture.width + x) * 4  # 4 channels (RGBA)
        image.pixels[index:index + 4] = color

# Draw lines between vertices
for i in range(len(vertices) - 1):
    draw_line(line_texture, vertices[i], vertices[i + 1], line_color)

# Optionally, close the loop by connecting the last vertex to the first vertex
draw_line(line_texture, vertices[-1], vertices[0], line_color)
