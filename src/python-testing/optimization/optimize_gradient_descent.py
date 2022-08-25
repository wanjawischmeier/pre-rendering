from cv2 import resize
import numpy as np
import cv2
from gradient_descent import gradient_descent as objective
from vector import *

def debug_triangle(x0: float3, x1: float3, x2: float3, iteration: int, max_iterations) -> None:
    global resolution, circumference, cost, rounding

    thickness = round(circumference / 400)
    relative_iterations = iteration / float(max_iterations)
    cost = cv2.line(cost, float2.as_tuple_tc(x0.xy, resolution), float2.as_tuple_tc(x1.xy, resolution), (relative_iterations, relative_iterations, relative_iterations), thickness)
    cost = cv2.line(cost, float2.as_tuple_tc(x0.xy, resolution), float2.as_tuple_tc(x2.xy, resolution), (relative_iterations, relative_iterations, relative_iterations), thickness)
    cost = cv2.line(cost, float2.as_tuple_tc(x1.xy, resolution), float2.as_tuple_tc(x2.xy, resolution), (relative_iterations, relative_iterations, relative_iterations), thickness)

    cost = cv2.circle(cost, float2.as_tuple_tc(x0.xy, resolution), thickness, (0, 1, 0), cv2.FILLED)
    cost = cv2.circle(cost, float2.as_tuple_tc(x1.xy, resolution), thickness, (1, 0, 0), cv2.FILLED)
    cost = cv2.circle(cost, float2.as_tuple_tc(x2.xy, resolution), thickness, (0, 0, 1), cv2.FILLED)

    # print(f"iteration: {iteration}\tsamples: {gradient_descent.samples}\terror: {rounding % x2.z}\tx0: ({rounding % x0.x}, {rounding % x0.y})")

    tmp = resize(cost, float2.as_tuple(resolution))
    cv2.imshow("cost", tmp)
    cv2.waitKey(1)

def debug_step(xn: float2, col: tuple[float, float, float]) -> None:
    global cost, circumference

    tmp = cost
    tmp = cv2.circle(tmp, float2.as_tuple_tc(xn.xy, resolution), round(circumference / 300), col, cv2.FILLED)
    
    tmp = resize(tmp, float2.as_tuple(resolution))
    # cv2.imshow("cost", tmp)
    # cv2.waitKey()

def nelder_mead(x0: float2, x1: float2, x2: float2, alpha: float=1, beta: float=0.5, gamma: float=2, max_iterations: int=10) -> float3:
    point = float2.random()
    
    # initialization
    b = float3.expand_float2(x0, objective(point, x0.x, x0.y))
    g = float3.expand_float2(x1, objective(point, x1.x, x1.y))
    w = float3.expand_float2(x2, objective(point, x2.x, x2.y))
    
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

        debug_triangle(b, g, w, i, max_iterations)

        if i > 0:
            point = float2.random()
        
        # midpoint
        m = (g + b) / 2

        debug_step(m, (0.2, 0.2, 0.2)) # gray

        # reflection
        r = m + alpha * float3.abs(m - w)
        fr = objective(point, r.x, r.y)

        debug_step(r, (1, 1, 0)) # cyan

        if fr < g.z:
            w = r
            w.z = fr
        
        else:
            if fr < w.z:
                w = r
                w.z = fr
            
            h = (w + m) / 2
            fh = objective(point, h.x, h.y)

            if fh < w.z:
                w = h
                w.z = fh
        
            debug_step(h, (1, 0, 1)) # magenta

        # expansion
        if fr < b.z:
            e = m + gamma * float3.abs(r - m)
            fe = objective(point, e.x, e.y)

            if fe < fr:
                w = e
                w.z = fe

            else:
                w = r
                w.z = fr

            debug_step(e, (0, 1, 1)) # yellow

        # contraction
        if fr > g.z:
            c = m + beta * float3.abs(w - m)
            fc = objective(point, c.x, c.y)

            if fc < w.z:
                w = c
                w.z = fc
        
            debug_step(c, (1, 1, 1)) # white

    return b




equilateral_triangle = 1 - 2 / 15
triangle_centroid_radius = 0.2
width = 600
heigth = 300
resolution = float2(width, heigth)
circumference = width + heigth
rounding = "%.8f"

x0 = float2(0.2, 0.4)

x = equilateral_triangle * triangle_centroid_radius
y = x0.y - 0.5 * triangle_centroid_radius

a = float2(x0.x - x, y)
b = float2(x0.x + x, y)
c = float2(x0.x, y + triangle_centroid_radius)

cost = np.zeros((heigth, width, 3))

best = float3.expand_float2(x0, 1)

for i in range(50):
    result = nelder_mead(a, b, c, max_iterations=25)

    if result.z < best.z:
        best = result

print(float3.as_tuple(best))