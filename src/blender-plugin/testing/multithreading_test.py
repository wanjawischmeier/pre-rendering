import bpy
import random
import timeit
import threading

class testThread(threading.Thread):
    def __init__(self):
        threading.Thread.__init__(self)

    def run(self):
        for i in range(1000):
            texture = bpy.data.images['Small']
            length = len(texture.pixels) - 1
            texture.pixels[random.randint(0, length)] = 1.0
        
def drawPoint():
    # print (f'Drawing point 2')
    texture = bpy.data.images['Small']
    length = len(texture.pixels) - 1
    texture.pixels[random.randint(0, length)] = 1.0

start_time = timeit.default_timer()
for i in range(5000):
    drawPoint()
print(f'Synchronous time: {timeit.default_timer() - start_time}')

threads: list[testThread] = []
for i in range(5):
    thread = testThread()
    threads.append(thread)
start_time = timeit.default_timer()
for thread in threads:
    thread.start()
    # thread.join()
print(f'Asynchronous time: {timeit.default_timer() - start_time}')