import cv2
import numpy as np
from math import sin, asin, cos, atan2, sqrt, pi

img_path = "images/test1/img1.jpg"
# fov = (81, 52)

def toPan(lat1, lon1, x, y, rx, ry, fov):
    x = pi * 2 * (x/rx - 0.5)
    y = pi * (y/ry - 0.5)

    p = sqrt(x * x + y * y)
    if p == 0: return 0, 0
    c = atan2(p, fov)
    lat = asin(cos(c) * sin(lat1) + y * sin(c) * cos(lat1) / p)
    lon = lon1 + atan2(x * sin(c), (p * cos(lat1) * cos(c) - y * sin(lat1) * sin(c)))

    px = lon / (pi * 2.0) + 0.5
    py = (lat / pi) + 0.5
    return round(px*rx%rx), round(py*ry%ry)

img = cv2.imread(img_path)
img = cv2.resize(img, (round(img.shape[1]/20), round(img.shape[0]/20)))

pan = np.zeros((200, 400, 3), np.uint8)
key = ord('a' )
lat = 0
lon = 0

while key != 32:
    pan = np.zeros((200, 400, 3), np.uint8)
    for y in range(img.shape[0]):   
        for x in range(img.shape[1]):
            px, py = toPan(lat, lon, x, y, pan.shape[1], pan.shape[0], 90/180*pi)
            pan[py][px] = img[y][x]

    cv2.imshow("RT Stitcher", pan)
    key = cv2.waitKey()
    if key == ord('w'): lat -= 0.1
    if key == ord('a'): lon -= 0.1
    if key == ord('s'): lat += 0.1
    if key == ord('d'): lon += 0.1

#cv2.imshow("RT TMP", img)
#cv2.waitKey()