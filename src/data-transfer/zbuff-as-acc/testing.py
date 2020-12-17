import wave

path = input("Enter path to wav file:\t")

with wave.open(path, 'rb') as wav:
    frames = wav.readframes(wav.getnframes())

    print(frames)

'''
Action          |   Value       |   Unit
__________________________________________
                    AAC std:
samplerate      |   44.100      |   Hz
max samplerate  |   48.000
samples p. s.   |   44.100      |   S/s
sample size     |   16          |   bits
bitrate         |   95.000      |   Bit/s
max bitrate     |   2
------------------------------------------
                    BW HD IMG:
bw hd image     |   921.600     |   bytes
framerate       |   30          |   fps
pixel size      |   8           |   bits
bytes - second  |   27.648.000  |   bytes
bits - second   |   221.184.000 |   Bit/s
------------------------------------------
                    BW 480p IMG:
bw hd image     |   172.800     |   bytes
framerate       |   30          |   fps
pixel size      |   8           |   bits
bytes - second  |   5.184.000   |   bytes
bits - second   |   41.472.000  |   Bit/s
------------------------------------------
                    needed:
samplerate      |   27.648.000  |   Hz
sample size     |   8           |   bits
bitrate         |   221.184.000 |   Bit/s
------------------------------------------
                    nested:
8-bit           |   256 (0-255) |   possib
16-bit          |   65.536      |   possib
32-bit          |   4294967296  |   possib


Iteration   |   max value   |   width
__________________________________________
                16 -> 8 bit
1st         |   255         |   65.280
2nd         |   255         |   16.711.680
'''