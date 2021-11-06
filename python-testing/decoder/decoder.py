import cv2
import numpy as np
from os import getcwd
from os.path import join

image_path = "testing/indexing/raw_lookup.png"
path = getcwd().split("pre-rendering")[0]
path = join(path, f"pre-rendering/master/renders/{image_path}")

w = 8
h = 4
img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
img = cv2.resize(img, (w, h))
up = cv2.resize(img, (1000, 500), interpolation=cv2.INTER_NEAREST)
flat = img.flatten()
low = np.uint8(flat)

x = 5
y = 2
m = 8
i = x + y * m
v0 = flat[i * 4]
v1 = img[y][x][0]

cv2.imshow("Image", up)
cv2.waitKey()