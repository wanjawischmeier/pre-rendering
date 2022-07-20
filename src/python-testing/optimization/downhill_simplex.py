from cv2 import resize
import numpy as np
import cv2
import objective
from vector import *
from objective import objective as f


def debug_triangle(x0: float2, x1: float2, x2: float2, iteration: int, max_iterations, error: float) -> None:
    global cost, res

    thickness = round(res / 400)
    relative_iterations = iteration / float(max_iterations)
    cost = cv2.line(cost, float2.as_tuple(x0), float2.as_tuple(x1), (relative_iterations, relative_iterations, relative_iterations), thickness)
    cost = cv2.line(cost, float2.as_tuple(x0), float2.as_tuple(x2), (relative_iterations, relative_iterations, relative_iterations), thickness)
    cost = cv2.line(cost, float2.as_tuple(x1), float2.as_tuple(x2), (relative_iterations, relative_iterations, relative_iterations), thickness)

    cost = cv2.circle(cost, float2.as_tuple(x0), thickness, (0, 1, 0), cv2.FILLED)
    cost = cv2.circle(cost, float2.as_tuple(x1), thickness, (1, 0, 0), cv2.FILLED)
    cost = cv2.circle(cost, float2.as_tuple(x2), thickness, (0, 0, 1), cv2.FILLED)

    # cost = cv2.putText(cost, f"samples:{objective.samples}", (20, 40), cv2.FONT_HERSHEY_SIMPLEX, 1, (1, 1, 1))
    print(f"iteration:{iteration}\tsamples:{objective.samples}\terror:{error}")

    tmp = resize(cost, (1600, 800))
    cv2.imshow("cost", tmp)
    cv2.waitKey()

def debug_step(xn: float2, col: tuple[float, float, float]) -> None:
    global cost, res

    tmp = cost
    tmp = cv2.circle(tmp, float2.as_tuple(xn), round(res / 300), col, cv2.FILLED)
    
    tmp = resize(tmp, (1600, 800))
    # cv2.imshow("cost", tmp)
    # cv2.waitKey()

def nelder_mead(x0: float2, x1: float2, x2: float2, alpha: float=1, beta: float=0.5, gamma: float=2, max_iterations: int=10):
    # initialization
    b = float3.expand_float2(x0, f(x0))
    g = float3.expand_float2(x1, f(x1))
    w = float3.expand_float2(x2, f(x2))
    
    for i in range(max_iterations):
        # sort
        if b.z > g.z:
            t = g
            g = b
            b = t
        
        if g.z > w.z:
            t = g
            g = w 
            w = t
            
            if b.z > g.z:
                t = g
                g = b
                b = t

        debug_triangle(b, g, w, i, max_iterations, b.z)
        
        # midpoint
        m = float3.round((g + b) / 2)
        fm = f(m)

        debug_step(m, (0.2, 0.2, 0.2)) # gray

        # reflection
        r = float3.round(m + alpha * (m - w))
        fr = f(r)

        debug_step(r, (1, 1, 0)) # cyan

        if fr < g.z:
            w = r
            w.z = fr
        
        else:
            if fr < w.z:
                w = r
                w.z = fr
            
            h = float3.round((w + m) / 2)
            fh = f(h)

            if fh < w.z:
                w = h
                w.z = fh
        
            debug_step(h, (1, 0, 1)) # magenta

        # expansion
        if fr < b.z:
            e = float3.round(m + gamma * (r - m))
            fe = f(e)

            if fe < fr:
                w = e
                w.z = fe

            else:
                w = r
                w.z = fr

            debug_step(e, (0, 1, 1)) # yellow

        # contraction
        if fr > g.z:
            c = float3.round(m + beta * (w - m))
            fc = f(c)

            if fc < w.z:
                w = c
                w.z = fc
        
            debug_step(c, (1, 1, 1)) # white





width = 1200
heigth = 600
res = width + heigth
objective.optimum = float2(0.4 * width, 0.6 * heigth)

x0 = float2.round(float2(0.8 * width, 0.45 * heigth))
x1 = float2.round(float2(0.85 * width, 0.35 * heigth))
x2 = float2.round(float2(0.9 * width, 0.5 * heigth))

x0 = float2.round(float2(0.5 * width, 0.45 * heigth))
x1 = float2.round(float2(0.55 * width, 0.35 * heigth))
x2 = float2.round(float2(0.6 * width, 0.5 * heigth))

cost = np.zeros((heigth, width, 3))

for y in range(heigth):
    for x in range(width):
        cost[y, x] = f(float2(x, y), False) / res

nelder_mead(x0, x1, x2, max_iterations=20)


