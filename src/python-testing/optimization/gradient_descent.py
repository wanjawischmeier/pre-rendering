from os import getcwd
from pathlib import Path
from math import sin, cos, acos, atan2, pi
import numpy as np
import cv2
from random import random
from vector import *

pi2 = pi * 2
KEYCODE_ESC = 27

def uv2ll(uv: float2) -> float2:
    return float2(
        uv.y * pi,
        uv.x * pi2 + pi
    )

def ll2uv(latLon: float2) -> float2:
    return float2(
        latLon.y / pi2 + 0.5,
        latLon.x / pi
    )

def translate_ll(latLon: float2, translation: float3, dist=1) -> float2:
    P = float3(
        dist * sin(latLon.y) * sin(latLon.x),
        dist * cos(latLon.x),
        dist * cos(latLon.y) * sin(latLon.x)
    )

    P += translation

    d = float3.magnitude(P)

    return float2(
        acos(P.y / d),
        atan2(P.x, P.z)
    )

def translate_uv(uv: float2, translation: float3, dist=1) -> float2:
    ll0 = uv2ll(uv)
    ll1 = translate_ll(ll0, translation, dist)
    return ll2uv(ll1)

def derrivative(x: float2, i: float2) -> float2:
    d = img[round(x.y * resolution.y), round(x.x * resolution.x), 3] / float(0xFFFF)
    uv = translate_uv(x, translation.__rmul__(-1), d)
    return i - uv, uv




def gradient_descent(x0: float2, learning_rate=0.3733, momentum=0.3, iterations=20, max_error=0, debugging=False):
    global samples, debug

    samples += 1
    x = x0
    adaptive_rate = float2(0, 0)

    for i in range(iterations):
        d = img[round(x.y * (resolution.y -1)), round(x.x * (resolution.x -1)), 3] / float(0xFFFF)
        uv = translate_uv(x, translation.__rmul__(-1), d)
        gradient = x0 - uv

        new_adaptive_rate = gradient.__rmul__(learning_rate) + adaptive_rate.__rmul__(momentum)
        adaptive_rate = new_adaptive_rate

        t = x
        x = (x + new_adaptive_rate) % 1

        if not debugging and i < iterations -1:
            continue

        f0 = float2.magnitude(gradient)

        if f0 < max_error:
            return f0

        if not debugging or not cv2.getWindowProperty('cost', cv2.WND_PROP_VISIBLE):
            continue

        print(f"iteration:{i}\terror:{rounding % f0}\tx:({rounding % x.x}, {rounding % x.y})\tgradient:({rounding % gradient.x}, {rounding % gradient.y})")

        debug = cv2.line(
            debug,
            float2.as_tuple(float2.round(t.__rmul__ (debug_resolution))),
            float2.as_tuple(float2.round(x.__rmul__(debug_resolution))),
            (random(), random(), random()), 4
        )

        cv2.imshow("cost", debug)
        
        if cv2.waitKey() == KEYCODE_ESC:
            return -1.0

    return f0


def sample_error(event, x, y, flags, param):
    global debug

    if not event == cv2.EVENT_LBUTTONDOWN:
        return
    
    tc = float2(x, y)
    uv = tc / debug_resolution
    d, uv1 = derrivative(uv, start)
    e = float2.magnitude(d)

    debug = cv2.circle(debug, float2.as_tuple(tc), 8, (e, e, e), cv2.FILLED)
    debug = cv2.line(
        debug,
        float2.as_tuple(tc),
        float2.as_tuple(float2.round(uv1.__rmul__(debug_resolution))),
        (random(), random(), random()), 4
    )
    
    print(f"user sample\terror:{rounding % e}\tx:({rounding % uv.x}, {rounding % uv.y})\tgradient:({rounding % d.x}, {rounding % d.y})")
    cv2.imshow("cost", debug)


def init(_resolution: float2, _debug_resolution: float2, _start: float2, _translation: float3, _file: str, _rounding: int=8) -> None:
    global resolution, debug_resolution, start, translation, rounding, samples, img
    global cost, debug

    resolution = _resolution
    debug_resolution = _debug_resolution
    start = _start
    translation = _translation
    rounding = f"%.{_rounding}f"
    samples = 0

    path = Path(getcwd()).parents[1].joinpath("renders", _file)
    img = cv2.imread(str(path), cv2.IMREAD_UNCHANGED)
    img = cv2.resize(img, float2.as_tuple(resolution))

if __name__ == "__main__":
    resolution = float2(400, 200)
    debug_resolution = float2(1000, 500)
    required_error = 1 / max(resolution.x, resolution.y)

    init(
        resolution,
        debug_resolution,
        float2(0.4, 0.75),
        float3(0.1, 0, 0),
        "cycles\\row_system\\room_simple_v2_270p\\0094.png"
    )
    
    cost = np.zeros((resolution.y, resolution.x, 3))

    for y in range(resolution.y):
        for x in range(resolution.x):
            uv = float2(x, y) / resolution
            d, _ = derrivative(uv, start)
            r = abs(d.x)
            g = abs(d.y)
            b = 0

            if d.x < 0 or d.y < 0:
                b = 1

            cost[y, x] = (b, g, r)

    debug = cv2.resize(cost, float2.as_tuple(debug_resolution))

    debug = cv2.circle(debug, float2.as_tuple(float2.round(start.__rmul__(debug_resolution))), 8, 1, cv2.FILLED)

    cv2.imshow("cost", debug)
    cv2.setMouseCallback("cost", sample_error)
    cv2.waitKey()
    gradient_descent(start, iterations=200, max_error=required_error, debugging=True)