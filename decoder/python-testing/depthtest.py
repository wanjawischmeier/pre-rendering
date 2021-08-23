import cv2
from os.path import join
from numpy import array, uint8

path = "S:\\users\\wanja\\Dokumente\\pre-rendering\\src\\decoder\\python-testing"

b8 = cv2.imread(join(path, "8b.jpg"))

cv2.imshow("8 Bits B", array(b8, dtype=uint8))
cv2.waitKey(0)