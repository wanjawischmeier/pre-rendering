def nest(input_list):
    arr = input_list
    c_index = int(arr[0])
    c_max = max + max * max

    for item in arr:
        c_max += max * c_max
        c_index += int(item) * c_max

    return c_index

mx = 255

v1 = 243
v2 = 132
v3 = 64
v4 = 200

print(v1, v2, v3, v4, mx)

m1 = mx * mx
i1 = v1 + v2 * mx

m2 = m1 * mx
i2 = i1 + v3 * mx

m3 = m2 * mx
i3 = i2 + v4 * mx

print(i1, i2, i3, m3)

r1 = i3 % m1
r2 = r1 % m2
r3 = r2 % m2
r4 = r3 % mx

r1 = i3 % (m3 * mx)
v1 = (i3 - r1) / (m3 * mx)

print(r1, v1)#, r2, r3, r4)