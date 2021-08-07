from numpy import arange, empty

sx = 0
sy = 0
sz = 0

mx = 10
my = 10
mz = 0

dx = mx - sx
dy = my - sy
dz = mz - sz

ss = 1
wd = max(dx, dy, dz)
sh = (dx + (dy + dz * wd) * wd, 3)

a = empty(sh)

for x in arange(sx, mx, ss):
    for y in arange(sy, my, ss):
        for z in arange(sz, mz, ss):
            a[(x + (y + z * wd) * wd, 3)]

print(a)