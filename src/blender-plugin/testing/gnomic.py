from math import atan, sqrt, sin, asin, cos

def gnomonicProjection(x, y, phi1, lambda0):
    p = sqrt(x * x + y * y)
    c = atan(p)

    phi = asin(cos(c) * sin(phi1) + (y * sin(c) * cos(phi1)) / p)
    _lambda = lambda0 + atan((x * sin(c)) / p * cos(phi1) * cos(c) - y * sin(phi1) * sin(c))

    return phi, _lambda
     

def inverseGnomonicProjection(phi, _lambda, phi1, lambda0):
    cos_c = sin(phi1) * sin(phi) + cos(phi1) * cos(phi) * cos(_lambda - lambda0)

    x = (cos(phi) * sin(_lambda - lambda0)) / cos_c
    y = (cos(phi1) * sin(phi) - sin(phi1) * cos(phi) * cos(_lambda - lambda0)) / cos_c

    return x, y

x = 0.8
y = 0.4
phi1 = 0
lambda0 = 0

phi, _lambda = gnomonicProjection(x, y, phi1, lambda0)
ox, oy = inverseGnomonicProjection(phi, _lambda, phi1, lambda0)

None