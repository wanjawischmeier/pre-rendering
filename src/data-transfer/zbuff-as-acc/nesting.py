def nest(input_list):
    arr = input_list
    c_index = int(arr[0])
    c_max = max + max * max

    for item in arr:
        c_max += max * c_max
        c_index += int(item) * c_max

    return c_index

'''
print(v1, v2, v3, v4, mx)

m1 = mx * mx
i1 = v1 + v2 * mx

m2 = m1 * mx
i2 = i1 + v3 * mx

m3 = m2 * mx
i3 = i2 + v4 * mx

print(i1, i2, i3, m3)

r1 = i3 % m1
r2 = r1 % m2
r3 = r2 % m2
r4 = r3 % mx

r1 = i3 % (mx)
v1 = (i3 - r1) / mx
'''

# i = x + y * w
# x = i % w
# y = (i - x) / w

# 256       - 65.536    - 4.294.967.296
# 33.903    - 4.195.503 - 1.065.403.503
#                       - 4.261.478.655
#                       - 4.195.503
'''
i1 = v1 + v2 * mx

r1 = i1 % mx
r2 = (i1 - r1) / mx

print(i1, r1, r2)
'''
mx = 255

v1 = 243
v2 = 132
v3 = 64
v4 = 200
'''
m1 = mx + mx**0
i1 = v1 + v2 * m1
m2 = mx + m1**2
i2 = i1 + v3 * m2
m3 = mx + m1**3
i3 = i2 + v3 * m3
m4 = mx + m1**4
i4 = i2 + v4 * m4

n4 = (i4 - 00) % m4
t4 = (i4 - n4) / m4
n3 = (i4 - n4) % m3
t3 = (i4 - n4) / m3

print(i1, i2, i3, i4)
print(n4, t4, n3, t3)


def to1d(x: int, y: int, z: int, mx: int) -> int:
    return (z * mx * mx) + (y * mx) + x

def to3d(idx: int, mx: int) -> []:
    z = idx / (mx * mx)
    idx -= (z * mx * mx)
    y = idx / mx
    x = idx % mx
    return x, y, z


bit = 8
idx = to1d(v1, v2, v3, 2**bit)
res = to3d(idx, 2**bit)

print([v1, v2, v3], idx, res)
'''
dx = 255; dy = 255; dz = 255      # dimensions
x1 = v1; y1 = v2; z1 = v3      # 3D point example
i = dx*dy*z1+dx*y1+x1       # corresponding 2D index

rx = i % dx                  # inverse transform recovering the x index
ry = ((i - rx)/dx) % dy     # inverse transform recovering the y index
rz = (i-rx -dx*ry)/(dx*dy)   # inverse transform recovering the z index

print(dx, dy, dz)
print(x1, y1, z1)
print(rx, ry, rz)
print(i)

def to1d(x: int, y: int, z: int, mx: int) -> int:
    return mx*mx*z+mx*y+x

def to3d(idx: int, mx: int) -> []:
    x = idx % mx
    y = ((idx - x) / mx) % mx
    z = (idx - x - mx * y) / (mx**2)

    return [x, round(y), round(z)]

idx = to1d(v1, v2, v3, mx)
mxi = to1d(mx, mx, mx, mx)
res = to3d(idx, mx)
'''
ren = []
for i in range(idx - 100, idx, 10):
    out = to3d(i, mx)
    ren.append([res[0] - out[0], res[1] - out[1], res[2] - out[2]])
rep = []
for i in range(idx, idx + 100, 10):
    out = to3d(i, mx)
    rep.append([res[0] - out[0], res[1] - out[1], res[2] - out[2]])
'''

print(
'''
Indexing:
-------------------------------
    max_val:\t%s
    in_val:\t%s
    index:\t%s
    max_index:\t%s
    out_val\t%s
-------------------------------
''' %(mx, [v1, v2, v3], idx, mxi, res)
)