from math import sqrt

def pack(a: int, b: int, percision: int) -> int:
    return (a << percision) | b
    
def unpack(c: int, percision: int) -> tuple:
    return (
        c >> percision,
        c & (2**percision -1)
    )

a = 15
b = 15
p = 4

c = pack(a, b, p)
a1, b1 = unpack(c, p)
print(a, b, c, p, a1, b1)
# am = int(a / 255 * 15)
# bm = int(b / 255 * 15)

# Store a and b into c
 #c = (a << 4) | b

# Extract a and b from c
# a1 = c >> 4
# b1 = c & 0xF

# a1m = a1 / 15 * 255
# b1m = b1 / 15 * 255