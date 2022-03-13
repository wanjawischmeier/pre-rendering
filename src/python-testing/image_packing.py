import numpy as np
import cv2

def addCaption(mat, txt):
    WHITE = (0xFFFF, 0xFFFF, 0xFFFF)
    BLACK = (0, 0, 0)
    font = cv2.FONT_HERSHEY_SIMPLEX
    font_size = 0.6
    font_thickness = 1
    x,y = 5,30
    mat = cv2.rectangle(mat, (5,5), (x+110,y+10), BLACK, -1)
    mat = cv2.putText(mat, txt, (x,y), font, font_size, WHITE, font_thickness, cv2.LINE_AA)

path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\renders\\cycles\\row_system\\room_simple_v2_270p\\"
path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\renders\\cycles\\row_system\\desert_new_540p\\"
file = "0222.png"
file = "0071.png"
sbit = 8
sfac = 2**sbit-1
i_start = 240
i_end = 245
t_start = 0
t_end = 0xFF
slope = (t_end - t_start) / (i_end - i_start)
mfac = round(0xFFFF / 0xFF)

img = cv2.imread(path + file, cv2.IMREAD_UNCHANGED)
# img = img[650:800, 450:650]
img = img[500:900, 400:700]
# img = cv2.resize(img, (160, 90))
dph = np.clip(img[:, :, 3], 0, 0xFFFF)

ref = np.array(dph / mfac, np.dtype("uint8"))
dp1 = np.array(dph, np.dtype("uint8"))
dp2 = np.array(dph, np.dtype("uint8"))
dp3 = np.array(dph)

for y in range(dph.shape[0]):
    for x in range(dph.shape[1]):
        val = dph[y, x]
        sva = t_start + round(slope * val)

        a = val >> sbit
        b = val & sfac
        c = (a << sbit) | b
        
        dph[y, x] = sva
        dp1[y, x] = a
        dp2[y, x] = b

cv2.imwrite("ref.png", ref)
cv2.imwrite("dp1.png", dp1)
cv2.imwrite("dp2.png", dp2)

ref = cv2.imread("ref.png", cv2.IMREAD_GRAYSCALE)
dp1 = cv2.imread("dp1.png", cv2.IMREAD_GRAYSCALE)
dp2 = cv2.imread("dp2.png", cv2.IMREAD_GRAYSCALE)

for y in range(dph.shape[0]):
    for x in range(dph.shape[1]):
        a = dp1[y, x]
        b = dp2[y, x]
        c = (a << sbit) | b
        
        dp3[y, x] = t_start + round(slope * c)
        
        rvl = ref[y, x]
        ref[y, x] = t_start + round(slope * rvl)

cv2.imwrite("ref.png", ref)

ref = np.uint16(ref) * mfac
dp1 = np.uint16(dp1) * mfac
dp2 = np.uint16(dp2) * mfac
dpm = cv2.merge([dp1, dp2, np.zeros(dp1.shape, dp1.dtype)])

img = cv2.cvtColor(img, cv2.COLOR_BGRA2BGR)
dph = cv2.cvtColor(dph, cv2.COLOR_GRAY2BGR)
ref = cv2.cvtColor(ref, cv2.COLOR_GRAY2BGR)
dp1 = cv2.cvtColor(dp1, cv2.COLOR_GRAY2BGR)
dp2 = cv2.cvtColor(dp2, cv2.COLOR_GRAY2BGR)
dp3 = cv2.cvtColor(dp3, cv2.COLOR_GRAY2BGR)

addCaption(dph, "16-bit")
addCaption(ref, "8-bit")
addCaption(dp1, "channel-1")
addCaption(dp2, "channel-2")
addCaption(dp3, "recombined")
addCaption(dpm, "channels")

left = cv2.vconcat([dph, ref, dp3])
right = cv2.vconcat([dp1, dp2, dpm])
cct = cv2.hconcat([left, right])
cct = cv2.resize(cct, (1200, 1000))

cv2.imwrite("overview.png", cct)
cv2.imshow("original", cct)
cv2.waitKey()