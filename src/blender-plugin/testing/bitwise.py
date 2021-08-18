a = 4
b = 6

# am = int(a / 255 * 15)
# bm = int(b / 255 * 15)

# Store a and b into c
c = (a << 4) | b

# Extract a and b from c
a1 = c >> 4
b1 = c & 0xF

# a1m = a1 / 15 * 255
# b1m = b1 / 15 * 255

print()