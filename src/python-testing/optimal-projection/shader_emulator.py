import cv2
import numpy as np
from time import time
from math import *

class float2:
    def __init__(self, x: float, y: float):
        self.x = x
        self.y = y

class float3:
    def __init__(self, x: float, y: float, z: float):
        self.x = x
        self.y = y
        self.z = z

class float4:
    def __init__(self, x: float, y: float, z: float, w: float):
        self.x = x
        self.y = y
        self.z = z
        self.w = w

    @property
    def as_tuple(self) -> tuple[float, float, float, float]:
        return (self.x, self.y, self.z, self.w)

    @property
    def rgb2bgr(self):
        return float4(
            self.z,
            self.y,
            self.x,
            self.w
        )

    def normalize(self, max_value: int):
        floating_max = float(max_value)

        return float4(
            self.x / floating_max,
            self.y / floating_max,
            self.z / floating_max,
            self.w / floating_max
        )

    def rescale(self, max_value: int):
        return float4(
            self.x * max_value,
            self.y * max_value,
            self.z * max_value,
            self.w * max_value
        )


class TextureFormat:
    uint8 = "uint8"
    uint16 = "uint16"

class Texture:
    def __init__(self, mat: np.array) -> None:
        self.mat = mat
        self.max_value: int = np.iinfo(mat.dtype).max

    def __init__(self, resolution: tuple[int, int], texture_format = TextureFormat.uint16) -> None:
        data_type = np.dtype(texture_format)
        self.mat = np.zeros((resolution[1], resolution[0], 4), data_type)
        self.max_value = np.iinfo(data_type).max

_textures: dict[str, Texture] = {}
_debug_textures: list[str] = []




def create_texture(name: str, resolution: tuple[int, int], texture_format = TextureFormat.uint16, debug = False) -> None:
    _textures[name] = Texture(resolution, texture_format)
    if debug: _debug_textures.append(name)

def load_texture(name: str, path: str, debug = False) -> None:
    img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    _textures[name] = Texture(img)
    if debug: _debug_textures.append(name)

def show_texture(name: str, wait=True, resolution=(-1, -1)) -> bool:
    tex = _textures[name].mat
    if resolution[0] > 0 and resolution[1] > 0:
        tex = cv2.resize(tex, resolution, cv2.INTER_NEAREST)
    cv2.imshow(name, tex)

    return cv2.waitKey(int(not wait)) != -1 # most readable line of code ever

def sample_texture(name: str, tc: tuple[int, int]) -> float4:
    height = _textures[name].mat.shape[0]
    col = _textures[name].mat[height - 1 - tc[1]][tc[0]]
    packed = float4(col[0], col[1], col[2], col[3]).rgb2bgr
    return packed.normalize(_textures[name].max_value)

def write_texture(name: str, tc: float2, value: float4) -> None:
    col = value.rescale(_textures[name].max_value).rgb2bgr.as_tuple
    height = _textures[name].mat.shape[0]
    _textures[name].mat[height - 1 - tc.y][tc.x] = col


def dispatch(kernel, dimensions: tuple[int, int], debug_fps=30, log=False) -> None:
    update_intervall = 1000 / float(debug_fps)
    last_update = round(time() * 1000)
    width, height = dimensions
    total_pixels = width * height

    for y in range(height):
        for x in range(width):
            id = float2(x, height - 1 - y)
            kernel(id, width, height)
            
            current_time = round(time() * 1000)
            if current_time < last_update + update_intervall:
                continue
            last_update = current_time
            
            if log:
                print(f"({x},\t{y})\t| ({x + y * width}\t/ {total_pixels})")

            for texture in _debug_textures:
                cancel = show_texture(texture, False)

        # exit nested loop, sure there's a proper way to do this
                if cancel:
                    break
            else:
                continue
            break
        else:
            continue
        break