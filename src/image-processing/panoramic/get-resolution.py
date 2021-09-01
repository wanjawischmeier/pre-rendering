from panutil import gnomonic
from math import ceil

sb = gnomonic((0, 0), (0, 0), 90)

scnw = 100
scnh = 100

sb = (sb[0] * scnw, sb[1] * scnh)

texr = (ceil(2 * sb[0] + scnw), ceil(2 * sb[0] + scnh))
print(texr)