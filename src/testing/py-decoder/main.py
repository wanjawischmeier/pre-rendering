from cv2 import imread, imshow, resize, split, waitKey, IMREAD_UNCHANGED

path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\unity-concept\\Assets\\Rendering\\Testing\\Sample1\\Main.png"

img = imread(path, IMREAD_UNCHANGED)
img = resize(img, (800, 400))
a = split(img)[3]

imshow("Image", img)
imshow("A", a)
waitKey(0)