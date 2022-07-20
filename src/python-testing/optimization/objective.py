import cv2
from math import sqrt
from vector import float2


optimum: float2
samples = 0

path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\renders\\cycles\\single\\single_cube\\540p\\left.png"
# img = cv2.imread(path, cv2.IMREAD_UNCHANGED)

def objective(x: float2, count_samples=True) -> float:
    global samples

    if count_samples:
        samples += 1
    
    p = float2(x.x - optimum.x, x.y - optimum.y)
    return float2.magnitude(p)

def objective2(x: float2, count_samples=True) -> float:
    global samples

    if count_samples:
        samples += 1
    
    # return float2.magnitude(p)