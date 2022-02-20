import fractions
import cv2


path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\branches\\master\\src\\unity\\concept\\Assets\\PreRendering\\100px_30fps_lowestq0001-0210.mp4"
cap = cv2.VideoCapture(path)

ret, frame = cap.read()
f = frame.flatten()

x = 4 # 22
y = 0 # 15
w = 100
c = 3
s = 10
i1 = (y+x*w)*c
i2 = (x+y*w)*c

print(frame[y][x])
print(i1, list(f)[i1:i1+c], i2, list(f)[i2:i2+c])

cv2.imshow("f", frame)
cv2.waitKey()   