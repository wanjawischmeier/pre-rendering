from math import sqrt
from mpl_toolkits import mplot3d
import numpy as np
import matplotlib.pyplot as plt
 
 
# function for z axea
def f(x, y):
    return np.sqrt(np.divide(x, y))

# x and y axis
iterations = 20
x = np.linspace(1, 100, iterations)
y = np.linspace(1, 10, iterations)
  
X, Y = np.meshgrid(x, y)
Z = f(X, Y)
 
fig = plt.figure()
ax = plt.axes(projection='3d')
ax.plot_surface(X, Y, Z, cmap='viridis')
ax.view_init(20, -145)

ax.set_xlabel("Seek Time (ms)")
ax.set_ylabel("Frame Time (ms)")
ax.set_zlabel("Chunk Width")
ax.set_title("Optimal Chunk Width\n(for different decoding and seeking performances)", size=20)
plt.show()