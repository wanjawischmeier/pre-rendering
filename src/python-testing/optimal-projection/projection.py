import numpy as np
from math import acos, atan2, pi, sin, cos, sqrt

radiant_conversion = (pi, pi * 2)

image: np.array
max_value: int
near_clip: float
far_clip: float
resolution: tuple[int, int]
position_offset: tuple[float, float, float]

def tuple_operation(a: tuple, b: tuple, op) -> tuple:
    return tuple(op(element_a, element_b) for element_a, element_b in zip(a, b))

def tuple_round(a: tuple[float, float]) -> tuple[int, int]:
    return (
        round(a[0]),
        round(a[1])
    )

def magnitude(a: tuple[float, float, float]) -> float:
    return sqrt(
        a[0]**2 +
        a[1]**2 +
        a[2]**2
    )

def setup(mat: np.array, clip: tuple[float, float], position: tuple[float, float, float]) -> tuple[int, int, int]:
    global image, max_value, far_clip, near_clip, resolution, position_offset
    image = mat
    max_value = np.iinfo(image.dtype).max
    near_clip, far_clip = clip

    height, width, _ = image.shape
    width -= 1
    height -= 1
    resolution = (width -1, height -1)
    position_offset = position

    return width, height, max_value


def project(id: tuple[int, int]) -> tuple[int, int]:
    uv = tuple_operation((id[1], id[0]), (resolution[1], resolution[0]), lambda a, b: a / b)
    ll1 = tuple_operation(uv, radiant_conversion, lambda a, b: a * b)
    ll1 = (ll1[0], ll1[1] + pi)

    col = image[id[1]][id[0]]
    d = col[3] / float(max_value)
    cp = d * (far_clip - near_clip) + near_clip

    p = (
        cp * sin(ll1[1]) * sin(ll1[0]),
        cp * cos(ll1[0]),
        cp * cos(ll1[1]) * sin(ll1[0])
    )
    p = tuple_operation(p, position_offset, lambda a, b: a + b)
        
    ll2 = (
        acos(p[1] / magnitude(p)),
        atan2(p[0], p[2])
    )

    a = tuple_operation(ll2, radiant_conversion, lambda a, b: a + b)
    a = (a[1] + 0.5, a[0])

    return tuple_round(tuple_operation(a, resolution, lambda a, b: a * b)), col