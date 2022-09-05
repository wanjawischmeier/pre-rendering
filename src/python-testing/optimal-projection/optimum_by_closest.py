from os import getcwd
from pathlib import Path
from math import pi, sin, cos, acos, atan2, sqrt
from time import time
import cv2

pi2 = pi * 2

def add_vector3(vector0: tuple[float, float, float], vector1: tuple[float, float, float]) -> tuple[float, float, float]:
    return (
        vector0[0] + vector1[0],
        vector0[1] + vector1[1],
        vector0[2] + vector1[2]
    )

def magnitude_vector2(vector: tuple[float, float]) -> tuple[float, float]:
    return sqrt(
        vector[0] * vector[0] +
        vector[1] * vector[1]
    )

def magnitude_vector3(vector: tuple[float, float, float]) -> tuple[float, float, float]:
    return sqrt(
        vector[0] * vector[0] +
        vector[1] * vector[1] +
        vector[2] * vector[2]
    )

def uv2ll(uv: tuple[float, float]) -> tuple[float, float]:
    return (
        uv[1] * pi,
        uv[0] * pi2
    )

def ll2uv(ll: tuple[float, float]) -> tuple[float, float]:
    return (
        ll[1] / pi2,
        ll[0] / pi
    )

def translateLatLon(latLon: tuple[float, float], translation: tuple[float, float, float], dist: float=1) -> tuple[float, float]:
    P = (
        dist * sin(latLon[1]) * sin(latLon[0]),
        dist * cos(latLon[0]),
        dist * cos(latLon[1]) * sin(latLon[0])
    )

    P = add_vector3(P, translation)

    return (
        acos(P[1] / magnitude_vector3(P)),
        atan2(P[0], P[2])
    )




geometry_resolution = (80, 45)
debug_resolution = (1280, 720)
width, height = geometry_resolution
file = r"cycles\row_system\room_simple_v2_540p\0094.png"
cache = r"src\python-testing\optimal-projection\optimum_cache.png"
cwd = Path(getcwd())
path = str(cwd.parents[1].joinpath("renders", file))
cache_path = str(cwd.joinpath(cache))
img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
img = cv2.resize(img, geometry_resolution)

for y0 in range(height):
    start = time()

    for x0 in range(width):
        # print(f"column: {x0}")
        u0 = x0 / width
        v0 = y0 / height
        err = width * height

        for y1 in range(height):
            for x1 in range(width):
                u1 = x1 / width
                v1 = y1 / height
                d = img[y1, x1, 3] / 0xFFFF

                uv1 = (u1, v1)
                ll1 = uv2ll(uv1)
                ll2 = translateLatLon(ll1, (0, 0, 0), d)
                uv2 = ll2uv(ll2)
                u2, v2 = uv2

                x2 = round(u2 * (width - 1))
                y2 = round(v2 * (height - 1))

                tmp = magnitude_vector2((
                    x0 - x2,
                    y0 - y2
                ))

                if tmp < err:
                    closest = (x2, y2)
                    err = tmp
        
        dbg = uv1
        x3, y3 = closest
        img[y3, x3] = (0, v0 * 0xFFFF, u0 * 0xFFFF, 0xFFFF)
    
    seconds = round(time() - start)
    remaining = (height - y0) * seconds
    if remaining < 60:
        remaining = f"{str(remaining)}s"
    else:
        remaining = f"{str(round(remaining / 60))}m"
    print(f"row: {y0}\t\t(took {seconds}s,\t~{remaining} remaining)")

cv2.imwrite(cache_path, img)
img = cv2.resize(img, debug_resolution, interpolation=cv2.INTER_NEAREST)
cv2.imshow("lol haha funny window title", img)
cv2.waitKey()