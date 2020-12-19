import wave, struct, math
from cv2 import (
    imread
)

path = 'C:\\Users\\wanja\\Documents\\dev\\csharp\\pre-rendering\\src\\data-transfer\\zbuff-as-acc\\'
'''
with wave.open(path + 'wavtest2.wav', 'w') as wav:
    wav.setnchannels(2)
    wav.setsampwidth(2)
    wav.setframerate(48000)

    for i in range(99999):
        value = round(math.sin(i / 100) * 10000)
        data = struct.pack('<h', value)
        wav.writeframes(data)
        value = round(math.sin(i / 50) * 20000)
        data = struct.pack('<h', value)
        wav.writeframes(data)
'''
    
with wave.open(path + 'wavtest2.wav', 'r') as wav:
    length = wav.getnframes()
    data = wav.readframes(length)
    print(data)

    print(length)
    print(len(data))
    print(data[12212])