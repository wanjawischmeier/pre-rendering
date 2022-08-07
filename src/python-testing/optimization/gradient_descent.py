from os import getcwd
from pathlib import Path
from math import sin, cos, acos, atan2, pi
import numpy as np
import cv2
from random import random
from vector import *

pi2 = pi * 2

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

"""
def objective(x: float2) -> float:
    d = x - optimum
    return float2.magnitude(d)

def derrivative(x: float2) -> float2:
    return optimum - x
"""

def derrivative(x: float2, i: float2) -> float2:
    d = img[round(x.y * resolution.y), round(x.x * resolution.x), 3] / float(0xFFFF)
    uv = translate_uv(x, translation.__rmul__(-1), d)
    return i - uv, uv




def gradient_descent(x0: float2, learning_rate=0.3733, momentum=0.3, iterations=20, debugging=False):
    global samples

    """
    xt = x0 + float2(0, learning_rate)
    xb = x0 - float2(0, learning_rate)
    xl = x0 + float2(learning_rate, 0)
    xr = x0 - float2(learning_rate, 0)

    f0 = f(x0)
    
    ft = f(xt)
    fb = f(xb)
    fl = f(xl)
    fr = f(xr)
    
    if ft < f0:
        x1 = xt
    elif fb < f0:
        x1 = xb
    elif fl < f0:
        x1 = xl
    elif fr < f0:
        x1 = xr
    
    x1 = x0 + learning_rate
    """
    
    samples += 1
    x = x0
    adaptive_rate = float2(0, 0)

    for i in range(iterations):
        """
        f0 = f(x0)
        f1 = f(x1)
        
        gradient = float2(
            (f0 - f(float2(x1.x, x0.y))) / (x0.x - x1.x),
            (f0 - f(float2(x0.x, x1.y))) / (x0.y - x1.y)
        )
        
        gradient = float2(
            f1 / (x0.x - x1.x),
            f1 / (x0.y - x1.y)
        )
        
        x0 = x1
        x1 -= learning_rate * gradient
        """

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

        if not debugging:
            continue

        print(f"iteration:{i}\terror:{rounding % f0}\tx:({rounding % x.x}, {rounding % x.y})\tgradient:({rounding % gradient.x}, {rounding % gradient.y})")

        debug = cv2.line(
            debug,
            float2.as_tuple(float2.round(t.__rmul__ (debug_resolution))),
            float2.as_tuple(float2.round(x.__rmul__(debug_resolution))),
            (random(), random(), random()), 4
        )

        cv2.imshow("cost", debug)
        cv2.waitKey()

    return f0


def sample_error(event, x, y, flags, param):
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

    """
    objective.init(None,
        float2(width, heigth),
        float2(0.5 * width, 0.5 * heigth)
    )
    """

    path = Path(getcwd()).parents[1].joinpath("renders", _file)
    img = cv2.imread(str(path), cv2.IMREAD_UNCHANGED)
    img = cv2.resize(img, float2.as_tuple(resolution))

if __name__ == "__main__":
    init(
        float2(400, 200),
        float2(1000, 500),
        float2(0.4, 0.75),
        float3(0.1, 0, 0),
        "cycles\\row_system\\room_simple_v2_270p\\0094.png"
    )
    
    cost = np.zeros((resolution.y, resolution.x, 3))

    for y in range(resolution.y):
        for x in range(resolution.x):
            # c = f(float2(x, y), False) / float(circumference)
            uv = float2(x, y) / resolution
            # c = objective(uv)
            d, _ = derrivative(uv, start)
            r = abs(d.x)
            g = abs(d.y)
            b = 0

            if d.x < 0 or d.y < 0:
                b = 1

            # cost[y, x] = (c, c, c)
            cost[y, x] = (b, g, r)

    debug = cv2.resize(cost, float2.as_tuple(debug_resolution))

    """
    cv2.imshow("cost", cost)
    cv2.waitKey()
    cv2.imwrite("src\\python-testing\\downhill-simplex\\simple_dst_gradient.png", cost)

    cost = cv2.imread("src\\python-testing\\downhill-simplex\\simple_dst_gradient.png")
    cv2.imshow("cost", cost)
    cv2.waitKey()
    """

    debug = cv2.circle(debug, float2.as_tuple(float2.round(start.__rmul__(debug_resolution))), 8, 1, cv2.FILLED)

    cv2.imshow("cost", debug)
    cv2.setMouseCallback("cost", sample_error)
    cv2.waitKey()
    gradient_descent(start, iterations=200, debugging=True)