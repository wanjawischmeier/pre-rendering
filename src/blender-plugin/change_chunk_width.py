import bpy

# chunkWidthOld = 4
heightOld = 21
chunkWidth = 4
chunkColumns = 5
chunkRows = 5
channelBlocks = 2

# not properly working yet
"""
def calculateFrameOffset(a,b):
    frame: int = bpy.context.scene.frame_current
    domainLocation = [0, 0]
    domainScale = [1, 1]

    chunkSize = chunkWidth**2
    blockWidth: int = domainScale[0]/chunkColumns/chunkWidth
    blockHeight: int = domainScale[1]/chunkRows/chunkWidth
    blocks = chunkColumns*chunkRows*chunkSize
    clampedFrame = frame%blocks
    domainOffset = (
        -domainScale[0]/2+domainLocation[0],
        -domainScale[1]/2+domainLocation[1]
    )
    
    chunkIndex = clampedFrame%chunkSize
    rowSize = chunkSize*chunkColumns

    chunkBoundsPosition = [
        (clampedFrame-chunkIndex)/chunkSize%chunkColumns*chunkWidth*blockWidth+domainOffset[0],
        (clampedFrame-clampedFrame%rowSize)/rowSize*chunkWidth*blockHeight+domainOffset[1]
    ]

    absolutePosition = [
        chunkBoundsPosition[0] + chunkIndex%chunkWidth*blockWidth,
        chunkBoundsPosition[1] + (chunkIndex-chunkIndex%chunkWidth)/chunkWidth*blockHeight
    ]

    localIndex = absolutePosition[0]%chunkWidthOld + absolutePosition[1]%(chunkWidthOld**2)
    chunkIndex = (frame-localIndex)/chunkWidthOld
    targetOffset = localIndex+chunkIndex

    bpy.app.driver_namespace['frameOffset'] = targetOffset

handler = bpy.app.handlers.frame_change_pre
handler.clear()
handler.append(calculateOldFrameOffset)
"""

node_tree = bpy.data.scenes['Scene'].node_tree
image_node = node_tree.nodes['Image']
mix_node_fac = node_tree.nodes["Mix"].inputs[0]

totalBlocks = chunkWidth**2*chunkColumns*chunkRows
chunkSize = chunkWidth**2
blocks = chunkColumns*chunkRows*chunkSize
frames = totalBlocks*channelBlocks-1

bpy.data.scenes[0].frame_end = frames

for frame in range(frames):
    clampedFrame = frame%blocks
        
    chunkIndex = clampedFrame%chunkSize
    rowSize = chunkSize*chunkColumns

    chunkBoundsPosition = [
        (clampedFrame-chunkIndex)/chunkSize%chunkColumns*chunkWidth,
        (clampedFrame-clampedFrame%rowSize)/rowSize*chunkWidth
    ]

    absolutePosition = [
        chunkBoundsPosition[0] + chunkIndex%chunkWidth,
        chunkBoundsPosition[1] + (chunkIndex-chunkIndex%chunkWidth)/chunkWidth
    ]
        
    targetOffset = absolutePosition[1]+absolutePosition[0]*heightOld

    mix_node_fac.default_value = (frame-frame%totalBlocks)/totalBlocks
    image_node.frame_offset = targetOffset-frame-1

    # drivers for the frame offset wouldn't update when rendering
    mix_node_fac.keyframe_insert("default_value", frame=frame)
    image_node.keyframe_insert("frame_offset", frame=frame)
