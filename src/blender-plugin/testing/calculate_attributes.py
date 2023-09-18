import bpy
import mathutils

layer_name = "all_facing_camera"

# Get the camera object
camera = bpy.data.objects.get("ChunkPosition")

# Get the object you want to process
object_name = "Wall"
obj = bpy.data.objects.get(object_name)

if obj is not None and obj.type == 'MESH':
    # Access the mesh data
    mesh = obj.data

    # Ensure custom data layers are enabled for vertices
    # mesh.use_customdata_vertex_bevel = True

    # Ensure the custom attribute exists
    if layer_name not in mesh.vertex_layers_int:
        mesh.vertex_layers_int.new(name=layer_name)

    # Access the custom attribute
    facing_layer = mesh.vertex_layers_int[layer_name]

    # Iterate over the vertices
    for vertex in mesh.vertices:
        # Calculate the direction from the camera to the vertex
        direction = camera.location - vertex.co
        # direction.normalize()

        adjacent_faces = [polygon for polygon in mesh.polygons if vertex.index in polygon.vertices]
        print(vertex.co, camera.location, direction, len(adjacent_faces))
        for face in adjacent_faces:
            face_normal = face.normal
            dot_product = face_normal.dot(direction)
            print(face_normal, dot_product)
            
            # If any adjacent face's normal faces away from the camera, set to 1 and break
            if dot_product < 0:
                facing_layer.data[vertex.index].value = 1
                break
        else:
            # If none of the adjacent faces' normals face away from the camera, set to 0
            facing_layer.data[vertex.index].value = 0