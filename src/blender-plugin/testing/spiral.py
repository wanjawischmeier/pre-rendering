# https://stackoverflow.com/questions/398299/looping-in-a-spiral
from numpy.lib.scimath import sqrt


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

spiral(2, 2)
print("--")
spiralI(4)