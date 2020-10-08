from json import dumps as serialize, loads as deserialize
from os.path import splitext
# from os import getcwd

def videoToMap(videoPath: str, targetPath: str, mapWidth: int):
    data = {
        "width":    mapWidth,
        "tstvalue": "whatever" 
    }

    data_bytes = bytearray(serialize(data), "utf-8")
    data_bytes.extend(bytearray("\n", "utf-8"))

    with open(videoPath, "rb") as videoBinary:
        data_bytes.extend(videoBinary.read())

    with open(splitext(targetPath)[0] + ".prm", "wb") as mapFile:
        mapFile.write(data_bytes)


def getMapData(mapPath: str, targetVideoPath: str) -> dict:
    with open(splitext(mapPath)[0] + ".prm", "rb") as binaryFile:
        binaryData = binaryFile.read()

    json_data, sep, video = binaryData.partition(bytearray("\n", "utf-8"))
    data = deserialize(json_data.decode("utf-8"))

    with open(targetVideoPath, "wb") as videoBinary:
        videoBinary.write(video)

    return data

"""
path = getcwd() + "\\src\\BlenderPlugin\\PreRendering"

videoToMap(path + "\\TestVideo.mp4", path + "\\TestMap.map", 24)
output = getMapData(path + "\\TestMap.map", path + "\\OutVideo.mp4")
"""