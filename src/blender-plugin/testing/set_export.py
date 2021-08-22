from bpy import context
from os.path import join

scene = context.scene
scene.use_nodes = True

tree = scene.node_tree

for node in tree.nodes:
    tree.nodes.remove(node)

render_node = tree.nodes.new(type='CompositorNodeRLayers')

out_node = tree.nodes.new(type='CompositorNodeOutputFile')
out_node.location = 500, 0
out_node.label = 'Output'
out_node.base_path = join(path, 'color')

format = out_node.format
format.color_mode = 'RGB'
format.color_depth = '16'

out_node.file_slots.remove(out_node.inputs[0])
out_node.file_slots.new('Color')
out_node.file_slots.new('Map')

links = tree.links
links.new(render_node.outputs['Image'], out_node.inputs['Color'])
links.new(render_node.outputs['Depth'], out_node.inputs['Map'])