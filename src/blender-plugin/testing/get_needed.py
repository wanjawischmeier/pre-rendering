from math import ceil
import numpy as np

def getNeeded(start: list, end: list, step_size: float) -> np.ndarray:
    needed = []
    for x in np.arange(start[0], end[0] + step_size, step_size):
        for y in np.arange(start[1], end[1] + step_size, step_size):
            for z in np.arange(start[2], end[2] + step_size, step_size):
                needed.append([x, y, z])
    return np.array(needed)

def getNeededLength(start: list, end: list, step_size: int) -> int:
    return (
        (end[0] + step_size - start[0]) *
        (end[1] + step_size - start[1]) *
        (end[2] + step_size - start[2])
    )

def getNeededLengthSimple(c: int, s: int) -> int:
    return (2 * c + s)**3

def getNeededRange(n: int, s: int) -> int:
    return ceil((n ** (1/3) - s) / 2)

c = 3
s = 1
n = getNeededLengthSimple(c, s)
n = 344
c2 = getNeededRange(n, s)

print(n, c2)