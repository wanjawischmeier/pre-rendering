m = 255
x1 = 1.2
y1 = 1.3
z1 = 1.4

i = x1 + (y1 + z1 * m) * m
mx = m + (m + m * m) * m

x2 = i % m
w2 = (i - x2) / m
y2 = w2 % m
z2 = (w2 - y2) / m

print(x1, y1, z1)
print(i, mx)
print(x2, y2, z2)