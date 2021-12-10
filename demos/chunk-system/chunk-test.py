from math import floor

def getCoordinates(x, y, w):
    return (
        floor(x/w),
        floor(y/w)
    )

def getPosition(x, y, w):
    return (
        x%w,
        y%w
    )

def getIndex(x, y, w):
    return x + y * w

# w: width of a chunk
# m: max chunks per row
def getChunkIndex(x, y, w, m):
    # Coordinates of the chunk
    chunkCoordinates = getCoordinates(x, y, w)

    # Coordinates relative to the chunk
    chunkPosition = getPosition(x, y, w)
    
    # Total index of the chunk
    chunkIndex = getIndex(chunkCoordinates[0], chunkCoordinates[1], m) * (w**2)

    # Index inside the chunk
    positionIndex = getIndex(chunkPosition[0], chunkPosition[1], w)

    return chunkIndex + positionIndex


def getCoordinates(i, w, m):
    y = i%m


chunkWidth = 5
chunkColumns = 3
x = 8
y = 7
r = 113

print(getChunkIndex(x, y, chunkWidth, chunkColumns))