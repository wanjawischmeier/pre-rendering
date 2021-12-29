import numpy as np
import matplotlib.pyplot as plt
from scipy.optimize.optimize import fmin


def f(x, sT, dT, mW=100):
    return sT*mW/x + dT*(x**2)

seekTime = 0.956
decodeTime = 0.073

m = fmin(f, seekTime/decodeTime, (seekTime, decodeTime))
m = round(m[0])
print(m)

viewRange = 10

x = [i for i in range(max(m-viewRange, 1), m+viewRange)]
y = [f(i, seekTime, decodeTime) for i in x]

plt.plot(x, y)
plt.show()