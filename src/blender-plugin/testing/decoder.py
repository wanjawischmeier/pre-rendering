import cv2

path = "S:\\users\\wanja\\Dokumente\\pre-rendering\\master\\src\\DllTest\\DllTest\\bin\\x64\\Debug\\netcoreapp3.1\\tstimg.png"

img = cv2.imread(path, cv2.IMREAD_UNCHANGED)
img = cv2.resize(img, (8, 4))
pass