import cv2
from math import acos, atan2, pi, sin, cos
from time import time
from os.path import join

import numpy as np
from vector import Vector2, Vector3

path = "src\\python-testing\\optimal-projection"
image_file = "left_50p.png"
img = cv2.imread(join(path, image_file), cv2.IMREAD_UNCHANGED)
imax = np.iinfo(img.dtype).max
out = np.zeros(img.shape, img.dtype)
height, width, _ = img.shape
res = Vector2(width -1, height -1)
rad = Vector2(pi, pi * 2)
fclip = 4
nclip = 2
show_tc = True
tres = (1920, 1080) # (960, 540)
fps = 10
update_intervall = 1000 / float(fps)
pos = Vector3(-1, 0, -1)

# from https://stackoverflow.com/a/34337534/13215204
cv2.namedWindow("projection", cv2.WND_PROP_FULLSCREEN)
cv2.setWindowProperty("projection", cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)

def project(id: Vector2) -> Vector2:
    ll1 = Vector2.multiply(Vector2.divide(id.yx, res.yx), rad)
    ll1.y += pi

    col = img[y][x]
    d = col[3] / float(imax)
    cp = d * (fclip - nclip) + nclip

    p = Vector3(
        cp * sin(ll1.y) * sin(ll1.x),
        cp * cos(ll1.x),
        cp * cos(ll1.y) * sin(ll1.x)
    )
    p = Vector3.add(p, pos) 
        
    ll2 = Vector2(
        acos(p.y / Vector3.magnitude(p)),
        atan2(p.x, p.z)
    )

    a = Vector2.divide(ll2, rad).yx
    a.x += 0.5

    return Vector2.round(Vector2.multiply(a, res)), col

last_update = round(time() * 1000)

for y in range(height):
    for x in range(width):
        id = Vector2(x, y)
        idx, col = project(id)

        if np.all(col[0:3] < 2) and show_tc:
            xN = x / float(width) * imax
            yN = 1 - y / float(height) * imax
            col = np.array([0, yN, xN, imax])
            img[y][x] = col
        
        tgt = out[idx.y][idx.x]

        if np.all(tgt[0:3] < 2):
            out[idx.y][idx.x] = col

        current_time = round(time() * 1000)

        if current_time < last_update + update_intervall:
            continue
        
        last_update = current_time
        cct = cv2.vconcat([img, out])
        cct = cv2.resize(cct, tres, interpolation=cv2.INTER_NEAREST)
        cv2.imshow("projection", cct)

        print(f"({x},\t{y})\t| ({x + y * width}\t/ {width * height})")

        if cv2.waitKey(1) != -1:
            break
    
    # exit nested loop
    else:
        continue
    break

cct = cv2.vconcat([img, out])
cct = cv2.resize(cct, tres, interpolation=cv2.INTER_NEAREST)
cv2.imshow("projection", cct)
cv2.imwrite(join(path, "projection.png"), cct)
cv2.waitKey()