def spiral(R):
    x = y = 0
    dx = 0
    dy = -1
    for i in range(R**2):
        if (-R/2 < x <= R/2) and (-R/2 < y <= R/2):
            print (x, y)
        if x == y or (x < 0 and x == -y) or (x > 0 and x == 1-y):
            dx, dy = -dy, dx
        x, y = x+dx, y+dy

spiral(3)