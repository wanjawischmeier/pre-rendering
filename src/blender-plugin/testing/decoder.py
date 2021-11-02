import cv2
import numpy as np
from os import getcwd
from os.path import join

path = getcwd().split("pre-rendering")[0]
path = join(path, "pre-rendering/master/renders/room_simple_v2_270p/0000.png")

w = 8
h = 4
img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
img = cv2.resize(img, (w, h))
# up = cv2.resize(img, (1000, 500), interpolation=cv2.INTER_NEAREST)
flat = img.flatten()

x = 5
y = 2
m = 8
i = x + y * m
v0 = flat[i * 4]
v1 = img[y][x][0]

cv2.waitKey()

"""
array([[[    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535]],

       [[    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535]],

       [[    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [55016, 54138, 46801, 33943],
        [56924, 56200, 49586, 33943],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535]],

       [[    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535],
        [13646,  7556,   287, 13918],
        [55318, 54492, 47340, 13918],
        [    1,     1,     1, 65535],
        [    1,     1,     1, 65535]]], dtype=uint16)
        """