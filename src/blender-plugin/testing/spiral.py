# https://stackoverflow.com/questions/398299/looping-in-a-spiral
from numpy.lib.scimath import sqrt
from numpy import ndarray, array, arange
from matplotlib.pyplot import plot, show

def spiral(X, Y):
    x = dx = 1
    y = dy = 0
    hX = X/2
    hY = Y/2

    for i in range(1, max(X, Y)**2):
        if (-hX < x <= hX) and (-hY < y <= hY):
            print (x, y)
            # DO STUFF...
        if x == y or (x < 0 and x == -y) or (x > 0 and x == 1-y):
            dx, dy = -dy, dx
        x, y = x+dx, y+dy

def spiralI(n):
    x = dx = 1
    y = dy = 0
    hW = sqrt(n+1)/2

    for i in range(1, n+1):
        if (-hW < x <= hW) and (-hW < y <= hW):
            print (x, y)
            # DO STUFF...
        if x == y or (x < 0 and x == -y) or (x > 0 and x == 1-y):
            dx, dy = -dy, dx
        x, y = x+dx, y+dy

def getNeeded(start: list, end: list, step_size: float) -> ndarray:
    needed = []
    for x in arange(start[0], end[0] + step_size, step_size):
        for y in arange(start[1], end[1] + step_size, step_size):
            for z in arange(start[2], end[2] + step_size, step_size):
                needed.append([x, y, z])
    return array(needed)
"""
spiral(2, 2)
print("--")
spiralI(4)

for c in range(1, 10):
    print(str(c) + ": " + str(len(getNeeded([-c, -c, 0], [c, c, 0], 1))))

points = [i for i in range(1, 20)]
values = [
    len(getNeeded([-c, -c, 0], [c, c, 0], 1)) for c in points
]

plot(points, values)
show()
def f(x):
    return (-1/1344)*x + (-3/224)*x + 2 + (1075/1344)
def f2(x):
    return round(0.043*x + 0.62)
def f3(x):
    return round(0.07*x + 0.405)
def f4(x):
    return round((-10E-3)*x + 0.05*x + 0.6)
def f5(x):
    return round((-1.2765522875817E-5)*x + 0.02218647875817*x + 2.654296875)
"""
# f(x) = ax² + bx + c
# P1(25, 2)
# P2(49, 3)
# P3(81, 4)
def f(x):
    if   x <= 9:   return 1
    elif x <= 25:  return 2
    elif x <= 49:  return 3
    elif x <= 81:  return 4
    elif x <= 121: return 5
    elif x <= 169: return 6
    elif x <= 225: return 7
    elif x <= 289: return 8
xs = [9, 25, 49, 81, 121, 169, 225, 289]
"""
1: 9
2: 25
3: 49
4: 81
5: 121
6: 169
7: 225
8: 289
9: 361
"""
"""
for c in range(1, 9):
    print(str(c) + ":\t" + str(len(getNeeded([-c, -c, 0], [c, c, 0], 1))))

for x in xs:
    print(str(x) + ":\t" + str(f(x)))
"""
# print(str(c) + ":\t" + str(len(getNeeded([-c, -c, 0], [c, c, 0], 1))))
# print(str(x) + ":\t" + str(f(x)))
def size(start: list, end: list, step_size: int) -> int:
    return (
        (end[0] + step_size - start[0]) *
        (end[1] + step_size - start[1]) *
        (end[2] + step_size - start[2])
    )

def getN(c: int, s: int) -> int:
    return (2 * c + s)**3

c = 2
start = [-1, -2, -3]
end = [4, 5, 6]
step_size = 1
i = 0
for x in arange(-1, 4 + 1):             # l1 = 4 + 1 - -1    |   end[0] + step_size - start[0]
        for y in arange(-2, 5 + 1):     # l2 = 5 + 1 - -2    |   end[1] + step_size - start[1]
            for z in arange(-3, 6 + 1): # l3 = 6 + 1 - -3    |   end[2] + step_size - start[2]
                i += 1                  # l = l1 ** l2 ** l3

s = size(start, end, step_size)

c = 2
start = [-c, -c, -c]
end = [c, c, c]
step_size = 1

print(size(start, end, step_size))
print(sizeS(c, step_size))