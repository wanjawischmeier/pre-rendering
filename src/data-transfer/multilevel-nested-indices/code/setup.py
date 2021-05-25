from extensions import *
from compression import *
from tkinter import Tk, Label, Button

#if createExtension(".test4", "Test Label Four", getExePath()): print("Created")
#else: print("Failed")
#print(getExePath())
test_list = [2, 5, 4, 3]
test_list_jagged = [[2, 4], [3, 5], [5, 8]]

compressed = compress('test2', test_list_jagged)
print(compressed)

decompressed = decompress('test1', compressed, 10)
print(decompressed)

input("Press enter to exit")