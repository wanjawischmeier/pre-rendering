from cv2 import imread, imshow, resize, split, waitKey, IMREAD_UNCHANGED

path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\unity\\concept\\Assets\\Rendering\\Testing\\Sample1\\Main.png"

img = imread(path, IMREAD_UNCHANGED)
img = resize(img, (8, 10))
a = split(img)[3]
b = img.flatten()
imshow("Image", img)
imshow("A", a)
waitKey(0)