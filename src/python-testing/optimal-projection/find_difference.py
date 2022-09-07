import cv2
import numpy as np

from os import getcwd
from pathlib import Path




file = r"src\python-testing\optimal-projection"
path = str(Path(getcwd()).joinpath(file))
db0 = cv2.imread(path + r"\dbo_0.png", cv2.IMREAD_UNCHANGED)
db1 = cv2.imread(path + r"\dbo_1.png", cv2.IMREAD_UNCHANGED)
db2 = cv2.imread(path + r"\dbo_2.png", cv2.IMREAD_UNCHANGED)
di1 = np.zeros(db0.shape, np.uint16)
di2 = np.zeros(db0.shape, np.uint16)
height, width, channels = db0.shape
debug_resolution_multiplier = 40
debug_width = 16 * debug_resolution_multiplier
debug_height = 9 * debug_resolution_multiplier
debug_resolution = (debug_width, debug_height)




for y in range(db0.shape[0]):
    for x in range(db0.shape[1]):
        _, y0, x0, _ = db0[y, x]
        _, y1, x1, _ = db1[y, x]
        _, y2, x2, _ = db2[y, x]

        di1[y, x] = (0, abs(y0 / 0xFFFF - y1 / 0xFFFF) * 250000, abs(x0 / 0xFFFF - x1 / 0xFFFF) * 250000, 0xFFFF)
        di2[y, x] = (0, abs(y0 / 0xFFFF - y2 / 0xFFFF) * 250000, abs(x0 / 0xFFFF - x2 / 0xFFFF) * 250000, 0xFFFF)


db0 = cv2.resize(db0, debug_resolution, interpolation=cv2.INTER_NEAREST)
db1 = cv2.resize(db1, debug_resolution, interpolation=cv2.INTER_NEAREST)
db2 = cv2.resize(db2, debug_resolution, interpolation=cv2.INTER_NEAREST)
di1 = cv2.resize(di1, debug_resolution, interpolation=cv2.INTER_NEAREST)
di2 = cv2.resize(di2, debug_resolution, interpolation=cv2.INTER_NEAREST)
cct = cv2.vconcat([di1, di2])
cv2.imshow("db0", db0)
cv2.imshow("db1", db1)
cv2.imshow("db2", db2)
cv2.imshow("cct", cct)
cv2.waitKey()