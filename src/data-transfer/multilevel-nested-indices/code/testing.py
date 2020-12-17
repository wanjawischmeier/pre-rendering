from compression import *
from random import randint
from sys import getsizeof as size

debug_mode = False

l1 = [2, 1, 8, 4]

length = 1000
l2 = [randint(0, 255) for i in range(length)]

out_ind = compress('mni', l2)

#print(out_ind)

out_l = decompress('mni', out_ind, length)

if debug_mode:
    print(
        f'''
        input 8-bit array:\t{l2}\t|\t(bytes required:\t{size(l2)})
        decompressed array:\t{out_l['result']}\t|\t(bytes saved:\t\t{size(l2) - size(out_ind)})
        '''
    )
else:
    print(
        f'''
        input: 8-bit array, bytes required:\t{size(l2)}
        bytes saved:\t\t\t\t{size(l2) - size(out_ind)}
        '''
    )

print(
    f'''
    compressed array:\t\t\t|\t(bytes required:\t{size(out_ind)})
    compression rate:\t{((size(l2) - size(out_ind)) *100) / size(l2)}%
    '''
)