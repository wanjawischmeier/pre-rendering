import cv2
import numpy as np
from shader_emulator.vectors import *


class TextureFormat:
    uint8 = "uint8"
    uint16 = "uint16"

class Texture:
    def __init__(self, mat: np.array):
        self.mat = mat
        self.max_value: int = np.iinfo(mat.dtype).max


_textures: dict[str, Texture] = {}
_debug_textures: list[str] = []


def create_texture(name: str, resolution: int2, texture_format = TextureFormat.uint16, debug = False) -> None:
    data_type = np.dtype(texture_format)
    shape = (resolution.y, resolution.x, 4)
    mat = np.zeros(shape, data_type)
    _textures[name] = Texture(mat)

    if debug: _debug_textures.append(name)


def load_texture(name: str, path: str, debug = False) -> None:
    img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    _textures[name] = Texture(img)

    if debug: _debug_textures.append(name)


def show_texture(name: str, wait=True, resolution=int2(-1, -1)) -> bool:
    tex = _textures[name].mat
    if resolution.x > 0 and resolution.y > 0:
        tex = cv2.resize(tex, resolution.as_tuple, cv2.INTER_NEAREST)
    cv2.imshow(name, tex)

    return cv2.waitKey(int(not wait)) != -1 # most readable line of code ever

def show_debug_textures() -> bool:
    for texture in _debug_textures:
        if show_texture(texture, False):
            return True

    return False


def sample_texture(name: str, tc: int2) -> float4:
    height: int = _textures[name].mat.shape[0]
    col: tuple = _textures[name].mat[height - 1 - tc.y][tc.x]
    packed = int4(col[0], col[1], col[2], col[3])
    return packed.normalize(_textures[name].max_value).rgb2bgr


def write_texture(name: str, tc: int2, value: float4) -> None:
    col = value.rgb2bgr.rescale(_textures[name].max_value).as_tuple
    height = _textures[name].mat.shape[0]
    _textures[name].mat[height - 1 - tc.y][tc.x] = col