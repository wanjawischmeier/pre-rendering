#import cv2
import numpy as np
from ui import Progressbar

global max
global ui_enabled
max = 255
ui_enabled = True
size = 2048

def __compress_1(input_list):
    keys = []
    values = []
    color_range = 255
    keys.append(color_range + color_range * color_range)
    for i in range(10):
        keys.insert(i +1, keys[i] + color_range * color_range)
    print(keys)

def __compress_mni(input_list):
    arr = np.array(input_list)
    #print('Compressing via mni (multilevel-nested-indices)')
    dimensions = len(arr.shape)
    # print(dimensions)
    if dimensions == 1: reshape = -1
    elif dimensions == 2:
        arr = arr.flatten()

    # print(arr)
    
    c_index = int(arr[0])#.to_bytes(size, 'big')
    c_max = max + max * max
    #arr = np.delete(arr, 0)

    if ui_enabled:
        bar = Progressbar('Compressing')
        bar.max = len(arr)

        for item in arr:
            c_max += max * c_max
            c_index += int(item) * c_max

            bar.step()
        
        bar.close()

    else:
        for item in arr:
            c_max += max * c_max
            c_index += int(item) * c_max

    return c_index


def __decompress_mni(index, lenght):
    result = []
    max_values = []

    max_values.append(max + max * max)

    for i in range(lenght): max_values.append(max_values[i] + max * max_values[i])

    for i in range(lenght):
        # max_values.append(max_values[i] + max * max_values[i])
        max_value = max_values[len(max_values) - (i +1)]

        temp = index % max_value
        value = (index - temp) / max_value
        result.insert(0, round(value))
        index = temp

    return {
        'max_values': max_values, 
        'result': result
    }

global compression_methods
compression_methods = {
    'test1': __compress_1, 
    'mni': __compress_mni # multilevel-nested-array
}

global decompression_methods
decompression_methods = {
    'mni': __decompress_mni
}

def compress(compression_method, input_list):
    compression_method = compression_methods.get(compression_method, 'Compression method not found')
    return compression_method(input_list)

def decompress(decompression_method, input_index, length):
    decompression_method = decompression_methods.get(decompression_method, 'Decompression method not found')
    return decompression_method(input_index, length)