import numpy as np
import cv2
import os
from os.path import basename, dirname, splitext, join, exists

sbit = 8
sfac = 2**sbit-1
bfac = 2**(sbit*2)-1
mfac = round(bfac / sfac)

def createMaps(file: str):
    img = cv2.imread(file, cv2.IMREAD_UNCHANGED)
    dwn = np.array(img[:, :, :3] / mfac, np.dtype("uint8"))
    dph = np.clip(img[:, :, 3], 0, 0xFFFF)
    dp1 = np.array(dph, np.dtype("uint8"))
    dp2 = np.array(dph, np.dtype("uint8"))

    for y in range(dph.shape[0]):
        for x in range(dph.shape[1]):
            val = dph[y, x]

            a = val >> sbit
            b = val & sfac
            c = (a << sbit) | b
            
            dp1[y, x] = a
            dp2[y, x] = b

    dpm = cv2.merge([dp1, dp2, np.zeros(dp1.shape, dp1.dtype)])
    dpc = cv2.cvtColor(dpm, cv2.COLOR_BGR2YCrCb)

    name = splitext(basename(file))[0]
    col_file = join(file_dir, f"{name}_col{file_ext}")
    dpm_file = join(file_dir, f"{name}_dph{file_ext}")
    # dp1_file = join(file_dir, f"raw_dp1{ext}")
    # dp2_file = join(file_dir, f"raw_dp2{ext}")
    # dpc_file = join(file_dir, f"dph_ycrcb{ext}")

    # cv2.imwrite(dp1_file, dp1)
    # cv2.imwrite(dp2_file, dp2)
    # cv2.imwrite(dpc_file, dpc)
    cv2.imwrite(col_file, dwn)
    cv2.imwrite(dpm_file, dpm)

folder = input("Drag and drop folder or enter full path: ")

file_ext = ".png"
mapname = splitext(basename(folder))[0]
file_dir = join(dirname(folder), f"{mapname}_maps")
if not exists(file_dir): os.makedirs(file_dir)

for file in os.listdir(folder):
    if file.endswith(file_ext):
        print(f"processing {file}")
        createMaps(join(folder, file))