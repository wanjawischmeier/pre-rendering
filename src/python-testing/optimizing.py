import numpy as np
import matplotlib.pyplot as plt
from math import sqrt
from scipy.optimize.optimize import fmin

"""
seekTime = 25
decodeTime = 4

def f(x, sT, dT, mW=100):
    return sT*mW/x + dT*(x**2)
    
m = fmin(f, seekTime/decodeTime, (seekTime, decodeTime))
m = round(m[0])
print(m)

viewRange = 10

x = [i for i in range(max(m-viewRange, 1), m+viewRange)]
y = [f(i, seekTime, decodeTime) for i in x]
"""

ft = 4  # frame-time
st = 24 # seek-time

def f(x, ft, st):
    return ft*(x**2)+st/x

def f2(x, ft, st):
    return (1/x)*ft*(x**2)+st*(1/x)

x = [i/10 for i in range(1, 100)]
y = [f2(i, ft, st) for i in x]

def s(ft, st):
    return (st/(2*ft))**(1/3)

# Thanks to https://www.wolframalpha.com/input/?i=local+minimum+calculator
def s2(ft, st):
    return sqrt(st/ft)

m1 = fmin(f2, 1, (ft, st))[0]
m2 = s2(ft, st) # (5/6)**(1/3)
print(m1, m2)

plt.plot(x, y)
plt.show()