import cv2
import os
# from plugin import variables, expressions

path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\renders\\cycles\\room_simple_v2_720p"
out = cv2.VideoWriter(os.path.join(path, "optimized.mp4"), cv2.VideoWriter_fourcc(*"h264"), 30, (5120, 1440))
# add pre-rendering\libraries to sys env vars

for root, dirs, files in os.walk(path):
    for name in files:
        if ".png" in name:
            file_path = os.path.join(root, name)
            mat = cv2.imread(file_path)
            out.write(mat)
            print(name)

out.release()