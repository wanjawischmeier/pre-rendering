import bpy
import mathutils

edge_attribute_name = "EdgeFacingCamera"
vertex_attribute_name = "HighlightVertexPosition"

# Get the camera object
camera_pos = bpy.data.objects.get("ChunkPosition")

# Get the object you want to process
obj = bpy.context.object

if obj is not None and obj.type == 'MESH':
    # Access the mesh data
    mesh = obj.data

    # Create a custom attribute for edges
    if edge_attribute_name not in mesh.attributes:
        mesh.attributes.new(edge_attribute_name, type="FLOAT", domain="EDGE")

    if vertex_attribute_name not in mesh.attributes:
        mesh.attributes.new(vertex_attribute_name, type="FLOAT_VECTOR", domain="POINT")

    # Access the custom attributes
    edge_facing_camera = mesh.attributes[edge_attribute_name]
    highlight_vertex_position = mesh.attributes[vertex_attribute_name]

    # Iterate over the edges
    for edge in mesh.edges:
        edge_pos = mesh.vertices[edge.vertices[0]].co
        direction = camera_pos.location - edge_pos

        facing_away_count = 0

        # Iterate over the polygons (faces)
        for polygon in mesh.polygons:
            # Check if the edge is part of the current polygon
            if edge.key in polygon.edge_keys:
                # Calculate the dot product between the polygon normal and the direction
                dot_product = polygon.normal.dot(direction)

                if dot_product <= 0:
                    facing_away_count += 1

        direction.normalize()
        vertexConstantDistance = camera_pos.location - 4 * direction
        
        for vertexIndex in edge.vertices:
            highlight_vertex_position.data[vertexIndex].vector = vertexConstantDistance

        # seperation only needed if exactly one is facing away
        if facing_away_count == 1:
            edge_facing_camera.data[edge.index].value = direction.length
        else:
            edge_facing_camera.data[edge.index].value = 0

    # Update attribute to reflect the changes in viewport
    mesh.attributes[edge_attribute_name].data.update()

    # https://blender.stackexchange.com/a/28689/173847
    bpy.ops.wm.redraw_timer(type='DRAW_WIN_SWAP', iterations=1)