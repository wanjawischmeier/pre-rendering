import cv2
from time import time

import numpy as np
from projection import setup, project

image_path = "src\\python-testing\\optimal-projection\\left_270p.png"
image = cv2.imread(image_path, cv2.IMREAD_UNCHANGED)
output = np.zeros(image.shape, image.dtype)
target_resolution = (1920, 1080) # (960, 540)
target_fps = 4
update_intervall = 1000 / float(target_fps)
position_offset = (-2, 0, -2)

width, height, max_value = setup(image, (2, 4), position_offset)

# from https://stackoverflow.com/a/34337534/13215204
cv2.namedWindow("projection", cv2.WND_PROP_FULLSCREEN)
cv2.setWindowProperty("projection", cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_FULLSCREEN)



last_update = round(time() * 1000)

for y in range(height):
    for x in range(width):
        id = (x, y)
        index, color = project(id)
        
        if np.any(color[0:3] < 2):
            x_normalized = x / float(width) * max_value
            y_normalized = y / float(height) * max_value
            color = np.array([0, y_normalized, x_normalized, max_value])
            image[y][x] = color

        output[index[1]][index[0]] = color

        current_time = round(time() * 1000)

        if current_time < last_update + update_intervall:
            continue
        
        last_update = current_time
        concatenated = cv2.vconcat([image, output])
        concatenated = cv2.resize(concatenated, target_resolution, interpolation=cv2.INTER_NEAREST)
        cv2.imshow("projection", concatenated)

        print(f"({x},\t{y})\t| ({x + y * width}\t/ {width * height})")

        if cv2.waitKey(1) != -1:
            break
    
    # exit nested loop
    else:
        continue
    break

concatenated = cv2.vconcat([image, output])
concatenated = cv2.resize(concatenated, target_resolution, interpolation=cv2.INTER_NEAREST)
cv2.imshow("projection", concatenated)
cv2.waitKey()