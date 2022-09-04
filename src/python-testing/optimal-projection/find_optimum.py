import cv2
import numpy as np

from os import getcwd
from pathlib import Path
from math import floor, sin, cos, acos, atan2, sqrt, pi

pi2 = pi * 2
KEYCODE_ESC = 27
KEYCODE_SPC = 32
NCLIP = 0.1
FCLIP = 30

def add_vec3(vector0: tuple[float, float, float], vector1: tuple[float, float, float]) -> tuple[float, float, float]:
    return (
        vector0[0] + vector1[0],
        vector0[1] + vector1[1],
        vector0[2] + vector1[2]
    )

def sub_vec2(vector0: tuple[float, float], vector1: tuple[float, float]) -> tuple[float, float]:
    return (
        vector0[0] - vector1[0],
        vector0[1] - vector1[1]
    )

def mag_vec2(vector: tuple[float, float]) -> tuple[float, float]:
    return sqrt(
        vector[0] * vector[0] +
        vector[1] * vector[1]
    )

def mag_vec3(vector: tuple[float, float, float]) -> tuple[float, float, float]:
    return sqrt(
        vector[0] * vector[0] +
        vector[1] * vector[1] +
        vector[2] * vector[2]
    )

def inv_vec3(vector: tuple[float, float, float]) -> tuple[float, float, float]:
    return sqrt(
        -vector[0],
        -vector[1],
        -vector[2],
    )

def uv2ll(uv: tuple[float, float]) -> tuple[float, float]:
    return (
        uv[1] * pi,
        uv[0] * pi2
    )

def uv2tc(vector: tuple[float, float], resolution: tuple[float, float]) -> tuple[float, float]:
    return (
        floor(vector[0] * resolution[0]),
        floor(vector[1] * resolution[1])
    )

def ll2uv(latLon: tuple[float, float]) -> tuple[float, float]:
    return (
        latLon[1] / pi2,
        latLon[0] / pi
    )

def translate_ll(latLon: tuple[float, float], translation: tuple[float, float, float], dist=1) -> tuple[float, float]:
    P = (
        dist * sin(latLon[1]) * sin(latLon[0]),
        dist * cos(latLon[0]),
        dist * cos(latLon[1]) * sin(latLon[0])
    )

    P = add_vec3(P, translation)

    d = mag_vec3(P)

    return (
        acos(P[1] / d),
        atan2(P[0], P[2])
    )

def translate_uv(uv: tuple[float, float], translation: tuple[float, float, float], dist=1) -> tuple[float, float]:
    ll0 = uv2ll(uv)
    ll1 = translate_ll(ll0, translation, dist)
    return ll2uv(ll1)

geometry_resolution = (80, 45)
debug_resolution = (640, 360)
translation = (-0.05, 0, 0)
pxl = (42, 24)
file = r"cycles\row_system\room_simple_v2_270p\0094.png"
path = str(Path(getcwd()).parents[1].joinpath("renders", file))

img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
img = cv2.resize(img, geometry_resolution)
dbg = np.zeros((geometry_resolution[1], geometry_resolution[0], 4), np.uint16)




for y in range(geometry_resolution[1]):
    for x in range(geometry_resolution[0]):
        tc0 = (x, y)
        d = img[y, x, 3] / 0xFFFF * (FCLIP - NCLIP) + NCLIP
        u = x / geometry_resolution[0]
        v = y / geometry_resolution[1]
        
        uv0 = (u, v)
        uv1 = translate_uv(uv0, translation)
        tc1 = uv2tc(uv1, geometry_resolution)

        val = uv0
        dbg[tc1[1], tc1[0]] = (0, val[1] * 0xFFFF, val[0] * 0xFFFF, 0xFFFF)




dbg[pxl[1], pxl[0]] = (0xFFFF, 0, 0xFFFF, 0xFFFF)
d = img[pxl[1], pxl[0], 3] / 0xFFFF * (FCLIP - NCLIP) + NCLIP
u = x / geometry_resolution[0]
v = y / geometry_resolution[1]
        
uv0 = (u, v)
uv1 = translate_uv(uv0, translation)
tc1 = uv2tc(uv1, geometry_resolution)

for y in range(geometry_resolution[1]):
    for x in range(geometry_resolution[0]):
        tc0 = (x, y)
        d = img[y, x, 3] / 0xFFFF * (FCLIP - NCLIP) + NCLIP
        u = x / geometry_resolution[0]
        v = y / geometry_resolution[1]
        
        uv0 = (u, v)
        uv1 = translate_uv(uv0, inv_vec3(translation))
        tc1 = uv2tc(uv1, geometry_resolution)

        # dst = mag_vec2(sub_vec2(tc1, ))

val = uv0
img[pxl[1], pxl[0]] = (0, val[1] * 0xFFFF, val[0] * 0xFFFF, 0xFFFF)
dbg[tc1[1], tc1[0]] = (0xFFFF, 0, 0, 0xFFFF)




img = cv2.resize(img, debug_resolution, interpolation=cv2.INTER_NEAREST)
dbg = cv2.resize(dbg, debug_resolution, interpolation=cv2.INTER_NEAREST)
cct = cv2.vconcat([img, dbg])
cv2.imshow("lol_cct", cct)


key = cv2.waitKey()
while key != KEYCODE_ESC and key != KEYCODE_SPC:
    key = cv2.waitKey()