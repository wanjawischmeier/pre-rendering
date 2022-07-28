import cv2
from math import pi, sin, cos, acos, atan2
from vector import *


width: float
height: float
circumference: float
optimum: float2
samples = 0
pi2 = pi * 2
img = None
offset = float3(0.5, 0, 0)


def init(path: str, res: float2, opt: float2):
    global width, height, circumference, optimum, img

    if path:
        img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
        img = cv2.resize(img, float2.as_tuple(res))
    
    width, height = float2.as_tuple(res)
    circumference = width + height
    optimum = opt


def uv2ll(uv: float2):
    return float2(
        uv.y * pi,
        uv.x * pi2
    )

def ll2uv(ll: float2):
    return float2(
        ll.y / pi2,
        ll.x / pi
    )

def translateLatLon(latLon: float2, translation: float3, dist: float=1):
    P = float3(
        dist * sin(latLon.y) * sin(latLon.x),
        dist * cos(latLon.x),
        dist * cos(latLon.y) * sin(latLon.x)
    )

    P += translation

    return float2(
        acos(P.y / float3.magnitude(P)),
        atan2(P.x, P.z)
    )

def objective(x: float2, count_samples=True) -> float:
    global samples

    if count_samples:
        samples += 1
    
    p = float2(x.x - optimum.x * width, x.y - optimum.y * height)
    return float2.magnitude(p) / circumference

def objective2(x: float2, count_samples=True) -> float:
    global samples, offset

    if count_samples:
        samples += 1
    
    d: float = img[x.y % height, x.x % width, 3] / float(0xFFFF)

    ll0 = uv2ll(x)
    ll1 = translateLatLon(ll0, offset, d)
    llt = uv2ll(optimum)

    return float2.magnitude(ll1 - llt) * 0.4