from math import sqrt
from vector import float2


opt = float2(300, 400)
samples = 0

def objective(x: float2, count_samples=True) -> float:
    global samples

    if count_samples:
        samples += 1
    
    p = float2(x.x - opt.x, x.y - opt.y)
    return float2.magnitude(p)