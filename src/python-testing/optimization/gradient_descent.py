import numpy as np
import cv2
import objective
from vector import *
from objective import objective as f

def gradient_descent(x0: float2, learning_rate=2, max_iterations=200):
    global cost
    """
    xt = x0 + float2(0, learning_rate)
    xb = x0 - float2(0, learning_rate)
    xl = x0 + float2(learning_rate, 0)
    xr = x0 - float2(learning_rate, 0)

    f0 = f(x0)
    
    ft = f(xt)
    fb = f(xb)
    fl = f(xl)
    fr = f(xr)
    
    if ft < f0:
        x1 = xt
    elif fb < f0:
        x1 = xb
    elif fl < f0:
        x1 = xl
    elif fr < f0:
        x1 = xr
    """
    
    x1 = x0 + learning_rate

    for i in range(max_iterations):
        f0 = f(x0)
        f1 = f(x1)
        
        gradient = float2(
            (f0 - f(float2(x1.x, x0.y))) / (x0.x - x1.x),
            (f0 - f(float2(x0.x, x1.y))) / (x0.y - x1.y)
        )
        """
        gradient = float2(
            f1 / (x0.x - x1.x),
            f1 / (x0.y - x1.y)
        )
        """
        x0 = x1
        x1 -= learning_rate * gradient

        print(f"iteration:{i}\terror:{f0}\tgradient:({gradient.x}, {gradient.y})")

        cost = cv2.line(
            cost,
            float2.as_tuple(float2.round(x0)),
            float2.as_tuple(float2.round(x1)),
            0, 4
        )

        cv2.imshow("cost", cost)
        cv2.waitKey()


width = 400
heigth = 200
res = width + heigth
objective.init(None,
    float2(width, heigth),
    float2(0.5 * width, 0.5 * heigth)
)

cost = np.zeros((heigth, width, 3))

for y in range(heigth):
    for x in range(width):
        c = f(float2(x, y), False) / float(res)
        cost[y, x] = (c, c, c)

"""
cv2.imshow("cost", cost)
cv2.waitKey()
cv2.imwrite("src\\python-testing\\downhill-simplex\\simple_dst_gradient.png", cost)

cost = cv2.imread("src\\python-testing\\downhill-simplex\\simple_dst_gradient.png")
cv2.imshow("cost", cost)
cv2.waitKey()
"""

gradient_descent(float2(0.7 * width, 0.3 * heigth))