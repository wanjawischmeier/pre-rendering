from cv2 import COLOR_GRAY2BGRA, imread, imshow, split, resize, vconcat, cvtColor, waitKey, IMREAD_UNCHANGED
from pg

path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\renders\\testing\\jpeg-2000\\test1.png"

img = imread(path, IMREAD_UNCHANGED)
img = resize(img, (1920, 540))
a = split(img)[3]
b = img.flatten()
imshow("Image", vconcat([img, cvtColor(a, COLOR_GRAY2BGRA)]))
waitKey(0)