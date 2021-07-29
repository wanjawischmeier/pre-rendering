from json import dumps
import numpy as np

a = {
    "resolution": 4096,
    "fclip": 10,
    "offsets": np.array([
        (1, 2, 3),
        (4, 5, 6),
        (7, 8, 9),
        (1, 2, 3)
    ]).ravel().tolist()
}

b = dumps(a)

print(b)