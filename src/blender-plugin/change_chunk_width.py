import bpy

chunkWidthOld = 4
chunkWidth = 4
chunkColumns = 5
chunkRows = 5

def calculateOldFrameOffset(a,b):
    frame: int = bpy.context.scene.frame_current

    x = frame%chunkWidth
    y = (frame-x)/chunkWidth

    localIndex = x%chunkWidthOld + y%(chunkWidthOld**2)
    chunkIndex = (frame-localIndex)/chunkWidthOld
    targetOffset = localIndex+chunkIndex-frame

    bpy.app.driver_namespace['frameOffset'] = targetOffset


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
    targetOffset = localIndex+chunkIndex-frame

    bpy.app.driver_namespace['frameOffset'] = targetOffset

handler = bpy.app.handlers.frame_change_pre
handler.clear()
handler.append(calculateFrameOffset)