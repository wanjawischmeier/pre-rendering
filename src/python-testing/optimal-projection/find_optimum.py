import cv2
import numpy as np

from os import getcwd
from time import time
from pathlib import Path
from math import floor, sin, cos, acos, atan2, sqrt, pi




pi2 = pi * 2
KEYCODE_ESC = 27
KEYCODE_SPC = 32
NCLIP = 0.1
FCLIP = 30
COLOR_BLUE =    (0xFFFF, 0, 0, 0xFFFF)
COLOR_GREEN =   (0, 0xFFFF, 0, 0xFFFF)
COLOR_RED =     (0, 0, 0xFFFF, 0xFFFF)
COLOR_TURQ =    (0xFFFF, 0xFFFF, 0, 0xFFFF)
COLOR_YELLOW =  (0, 0xFFFF, 0xFFFF, 0xFFFF)
COLOR_MAGENTA = (0xFFFF, 0, 0xFFFF, 0xFFFF)
COLOR_WHITE =   (0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF)




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

def mag_vec2(vector: tuple[float, float]) -> float:
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
    return (
        -vector[0],
        -vector[1],
        -vector[2]
    )

def swp_vec2(vector: tuple[float, float]) -> tuple[float, float]:
    return (
        vector[1],
        vector[0],
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

def tc2uv(vector: tuple[float, float], resolution: tuple[float, float]) -> tuple[float, float]:
    return (
        vector[0] / resolution[0],
        vector[1] / resolution[1]
    )

def ll2uv(latLon: tuple[float, float]) -> tuple[float, float]:
    return (
        latLon[1] / pi2,
        latLon[0] / pi
    )

def translate_ll(latLon: tuple[float, float], translation: tuple[float, float, float], dist=1) -> tuple[tuple[float, float], float]:
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
    ), d

def translate_uv(uv: tuple[float, float], translation: tuple[float, float, float], dist=1) -> tuple[tuple[float, float], float]:
    ll0 = uv2ll(uv)
    ll1, d = translate_ll(ll0, translation, dist)
    return ll2uv(ll1), d




debug_fps = 0
if debug_fps > 0: debug_hz = 1 / debug_fps
geometry_resolution_multiplier = 10
debug_resolution_multiplier = 40
geometry_width = 16 * geometry_resolution_multiplier
geometry_height = 9 * geometry_resolution_multiplier
debug_width = 16 * debug_resolution_multiplier
debug_height = 9 * debug_resolution_multiplier
geometry_resolution = (geometry_width, geometry_height)
debug_resolution = (debug_width, debug_height)
translation = (2, 0, 0)
pxl = (4 * geometry_resolution_multiplier, 6 * geometry_resolution_multiplier)
file = r"cycles\row_system\room_simple_v2_270p\0094"
cache_dir = r"src\python-testing\optimal-projection"
file_format = ".png"
iteration = 2
ending = f"_{iteration}{file_format}"
cwd = Path(getcwd())
path = str(cwd.parents[1].joinpath("renders", file + file_format))
cache = cwd.joinpath(cache_dir)

img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
img = cv2.resize(img, geometry_resolution)
dbg = np.zeros((geometry_height, geometry_width, 4), np.uint16)
dbo = np.zeros((geometry_height, geometry_width, 4), np.uint16)
cbo = np.zeros((geometry_height, geometry_width, 4), np.uint16)




for y1 in range(geometry_height):
    for x1 in range(geometry_width):
        tc0 = (x1, y1)
        d = img[y1, x1, 3] / 0xFFFF * (FCLIP - NCLIP) + NCLIP
        
        uv0 = tc2uv(tc0, geometry_resolution)
        uv1, _ = translate_uv(uv0, translation, d)
        tc1 = uv2tc(uv1, geometry_resolution)

        col = (0, uv0[1] * 0xFFFF, uv0[0] * 0xFFFF, 0xFFFF)
        dbg[swp_vec2(tc1)] = col
        if not any(dbg[swp_vec2(tc0)]):
            dbg[swp_vec2(tc0)] = col




# cv2.circle(dbg, swp_vec2(pxl), 2, (0xFFFF, 0, 0xFFFF, 0xFFFF), cv2.FILLED)
x1, y1 = pxl
d = img[y1, x1, 3] / 0xFFFF * (FCLIP - NCLIP) + NCLIP
u = x1 / geometry_width
v = y1 / geometry_height

uv0 = tc2uv(pxl, geometry_resolution)
uv1, d = translate_uv(uv0, translation, d)
tc1 = uv2tc(uv1, geometry_resolution)

x1, y1 = tc1
d = img[y1, x1, 3] / 0xFFFF * (FCLIP - NCLIP) + NCLIP
uv3, _ = translate_uv(uv1, inv_vec3(translation), d)
tc3 = uv2tc(uv3, geometry_resolution)

odt = mag_vec2(sub_vec2(tc3, pxl))
# cv2.circle(dbg, tc1, round(odt), COLOR_TURQ, cv2.FILLED)

cv2.line(dbg, pxl, tc1, COLOR_BLUE, 1)
cv2.line(dbg, tc1, tc3, COLOR_RED, 1)
cv2.circle(dbg, pxl, 2, COLOR_BLUE, cv2.FILLED)
cv2.circle(dbg, tc1, 2, COLOR_TURQ, cv2.FILLED)
cv2.circle(dbg, tc3, 2, COLOR_RED, cv2.FILLED)




dst = geometry_width * geometry_height
opt = pxl
otc = tc1

for y1 in range(geometry_height):
    for x1 in range(geometry_width):
        tc0 = (x1, y1)
        d = img[y1, x1, 3] / 0xFFFF * (FCLIP - NCLIP) + NCLIP
        u = x1 / geometry_width
        v = y1 / geometry_height
        
        uv0 = (u, v)
        uv1, _ = translate_uv(uv0, inv_vec3(translation), d)
        tc1 = uv2tc(uv1, geometry_resolution)

        tmp = mag_vec2(sub_vec2(tc1, pxl))
        if tmp < dst:
            opt = tc0
            lt1 = tc1
            dst = tmp
        if tc0 == otc:
            cv2.line(dbg, tc0, tc1, COLOR_WHITE, 1)

val = uv0
# (0, val[1] * 0xFFFF, val[0] * 0xFFFF, 0xFFFF)
# cv2.circle(dbg, opt, round(dst), COLOR_GREEN, cv2.FILLED)
cv2.line(dbg, opt, lt1, COLOR_MAGENTA, 1)
cv2.circle(dbg, opt, 1, COLOR_GREEN, cv2.FILLED)
# cv2.circle(dbg, lt1, 1, COLOR_GREEN, cv2.FILLED)



try:
    for y0 in range(geometry_height):
        if y0 < geometry_height / 3:
            continue
        start = time()

        for x0 in range(geometry_width):
            if x0 > geometry_width / 2:
                continue
            # print(f"column: {x0}")
            dst = geometry_width * geometry_height
            tgt = (x0, y0)
            key = 0
            last_update = time()

            for y1 in range(geometry_height):
                for x1 in range(geometry_width):
                    tc0 = (x1, y1)
                    d = img[y1, x1, 3] / 0xFFFF * (FCLIP - NCLIP) + NCLIP
                    u = x1 / geometry_width
                    v = y1 / geometry_height
                    
                    uv0 = (u, v)
                    uv1, _ = translate_uv(uv0, inv_vec3(translation), d)
                    tc1 = uv2tc(uv1, geometry_resolution)

                    tmp = mag_vec2(sub_vec2(tc1, tgt))
                    if tmp < dst:
                        opt = tc0
                        lt1 = tc1
                        dst = tmp
            
            dbo[swp_vec2(tgt)] = (0, opt[1] / geometry_height * 0xFFFF, opt[0] / geometry_width * 0xFFFF, 0xFFFF)
            # d = img[swp_vec2(opt)][3]
            # cbo[swp_vec2(tgt)] = (d, d, d, 0xFFFF)
            cbo[swp_vec2(tgt)] = img[swp_vec2(opt)]
            current_time = time()
            if debug_fps == 0 or (debug_fps > 0 and current_time - last_update > debug_hz):
                last_update = current_time
                cuo = cv2.resize(cbo, debug_resolution, interpolation=cv2.INTER_NEAREST)
                duo = cv2.resize(dbo, debug_resolution, interpolation=cv2.INTER_NEAREST)
                cct = cv2.vconcat([cuo, duo])
                cv2.imshow("progress", cct)
                key = cv2.waitKey(1)
                if key == KEYCODE_ESC:
                    raise StopIteration
                if key == KEYCODE_SPC:
                    break

        seconds = round(time() - start)
        remaining = (geometry_height - y0) * seconds
        if remaining < 60:
            remaining = f"{str(remaining)}s"
        else:
            remaining = f"{str(round(remaining / 60))}m"
        print(f"row: {y0}\t\t(took {seconds}s,\t~{remaining} remaining)")

except StopIteration:
    print(f"terminated loop by user input (at iteration x0={x0},y0={y0})")
cv2.destroyWindow("progress")




cv2.imwrite(str(cache.joinpath("dbg" + ending)), dbg)
cv2.imwrite(str(cache.joinpath("cbo" + ending)), cbo)
cv2.imwrite(str(cache.joinpath("dbo" + ending)), dbo)
img = cv2.resize(img, debug_resolution, interpolation=cv2.INTER_NEAREST)
dbg = cv2.resize(dbg, debug_resolution, interpolation=cv2.INTER_NEAREST)
cbo = cv2.resize(cbo, debug_resolution, interpolation=cv2.INTER_NEAREST)
dbo = cv2.resize(dbo, debug_resolution, interpolation=cv2.INTER_NEAREST)
ccl = cv2.vconcat([img, dbg])
ccr = cv2.vconcat([cbo, dbo])
cct = cv2.hconcat([ccl, ccr])
cv2.imwrite(str(cache.joinpath("cct" + ending)), cct)
cv2.imshow("lol_cct", cct)

key = cv2.waitKey()
while key != KEYCODE_ESC and key != KEYCODE_SPC:
    key = cv2.waitKey()