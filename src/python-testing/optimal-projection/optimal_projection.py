import cv2
from math import acos, atan2, pi, sin, cos

import numpy as np
from vector import Vector2, Vector3

path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\branches\\master\\src\\python-testing\\optimal-projection\\left.png"
img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
out = np.copy(img)
width, height, _ = img.shape
res = Vector2(width, height)
rad = Vector2(pi, pi * 2)
fclip = 4
nclip = 2

for y in range(height):
    for x in range(width):
        id = Vector2(x, y)

        ll1 = Vector2.multiply(Vector2.divide(id / res) * rad)
        ll1.x += pi

        col = img[y][x]
        cp = col[3] * (fclip - nclip) + nclip

        p = Vector3(
            cp * sin(ll1.y) * sin(ll1.x),
            cp * cos(ll1.x),
            cp * cos(ll1.y) * sin(ll1.x)
        )

        ll2 = Vector2(
            acos(p.y / p.magnitude),
            atan2(p.x, p.z)
        )

        a = (ll2 / rad).yx
        a.x += 0.5

        idx = Vector2.round(Vector2.multiply(a, res))

        out[idx.y][idx.x] = col

