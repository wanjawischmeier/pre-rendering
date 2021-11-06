from json import dumps
import numpy as np

a = {
    "resolution": 4096,
    "fclip": 10,
    "mx_width": 10,
    "offsets": np.array([
        (1, 2, 3),
        (4, 5, 6),
        (7, 8, 9),
        (1, 2, 3)
    ]).ravel().tolist()
}

# b = dumps(a)

def getNeeded(start: tuple, end: tuple, step_size: float) -> list:
    needed = []
    for x in range(start[0], end[0], step_size):
        for y in range(start[1], end[1], step_size):
            for z in range(start[2], end[2], step_size):
                needed.append((x, y, z))
    return needed

n = getNeeded((0, 0, 0), (10, 10, 0), 1)
print(n)