from math import (
    sin, asin, cos, atan2, sqrt, pi
)

def gnomonic(position: tuple, rotation: tuple, fov: float) -> tuple:
    pi2 = pi * 2

    x = pi2 * (position[0] - 0.5)
    y = pi * (position[1] - 0.5)

    p = sqrt(x * x + y * y)
    c = atan2(p, fov)

    sinC = sin(c)
    cosC = cos(c)
    sinPhi1 = sin(rotation[0])
    cosPhi1 = cos(rotation[0])

    phi = asin(cosC * sinPhi1 + y * sinC * cosPhi1 / p)
    _lambda = rotation[1] + atan2(x * sinC, (p * cosPhi1 * cosC - y * sinPhi1 * sinC))

    return (_lambda / pi2 + 0.5, (phi / pi) + 0.5)

