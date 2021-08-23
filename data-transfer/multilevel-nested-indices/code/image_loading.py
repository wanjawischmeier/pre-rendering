import cv2
from compression import compress, decompress
from timeit import default_timer as time
from numpy import reshape, asarray
from sys import getsizeof as size

resolution = (128, 72)
resolution = (200, 100)

img = cv2.imread('other/testimg.jpg', cv2.IMREAD_GRAYSCALE)
small = cv2.resize(img, resolution)

l = []
for i in range(len(small)):
    for j in range(len(small[i])):
        l.append(small[i][j])

c_time = time()
idx = compress('mni', small)
c_time = time() - c_time
print(c_time)

d_time = time()
out = decompress('mni', idx, len(l))
d_time = time() - d_time
print(d_time)

out = out['result']
out_img = asarray(out, dtype = 'uint8')
out_img = reshape(out_img, (resolution[1], resolution[0]))
'''
for i in range(len(small)):
    for j in range(len(small[i])):
        ind = i + j * small.shape[0]
        out_img[i][j] = out[ind]
'''

small = cv2.resize(small, (640, 360))
out_img = cv2.resize(out_img, (640, 360))

small = cv2.rectangle(
    small, 
    (0, 0), 
    (640, 30), 
    0, 
    -1
)

small = cv2.putText(
    small, 
    f'Original ({round(size(small) /1000)} kilobytes)', 
    (10, 20), 
    cv2.FONT_HERSHEY_SIMPLEX, 
    0.6, 
    255, 
    1
)


out_img = cv2.rectangle(
    out_img, 
    (0, 0), 
    (640, 30), 
    0, 
    -1
)

out_img = cv2.putText(
    out_img, 
    f'Compressed ({round(size(out) /1000)} kilobytes, {round(c_time, 2)}sec -> {round(d_time, 2)}sec)', 
    (10, 20), 
    cv2.FONT_HERSHEY_SIMPLEX, 
    0.6, 
    255, 
    1
)

output = cv2.vconcat([small, out_img])

cv2.imshow('Compression', output)

cv2.waitKey()