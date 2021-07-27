from matplotlib import pyplot as plt
from math import log
import numpy as np

min = 0.1
max = 1

a = [i for i in np.arange(min, max, 0.1)]
b = [max-min/i for i in a]
c = [min/(max-i) for i in b]

fig, axs = plt.subplots(3)
fig.suptitle("Normalization")
axs[0].set_title("a = np.arange(min, max, min)")
axs[0].plot(a, a)
axs[1].set_title("b = max-min/a")
axs[1].plot(a, b)
axs[2].set_title("c = min/(max-b)")
axs[2].plot(a, c)
for ax in axs.flat:
    ax.label_outer()

plt.show()