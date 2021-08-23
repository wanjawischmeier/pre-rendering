from subprocess import check_output, call, check_call, Popen, PIPE, STDOUT
from sys import argv
import re

if len(argv) == 3: 
    ffmpeg_exe = argv[1]
    file_path = argv[2]

else:
    ffmpeg_exe = input("ffmpeg_exe: ")
    file_path = input("file_path: ")
# ffmpeg.exe -i C:\Users\User\Documents\Blender\out_libx264_interframe.mp4 -vf select='eq(n,34)',showinfo -f null -

# C:\ProgramData\ffmpeg\bin\ffmpeg.exe -i C:\Users\User\Documents\Blender\interframe.mp4 -vf select='eq(n, 31)', showinfo -f null -
# C:\ProgramData\ffmpeg\bin\ffmpeg.exe -i C:\Users\User\Documents\Blender\interframe.mp4 -vf select='eq(n,34)',showinfo -f null -
# C:\ProgramData\ffmpeg\bin\ffmpeg.exe -i C:\Users\User\Documents\Blender\interframe.mp4 -vf select='eq(n, 31)',showinfo -f null -

# command = f"{ffmpeg_exe} -i {file_path} -vf select='eq(n,31)',showinfo -f null -"
command = f"{ffmpeg_exe} -i {file_path} -vf select='n',showinfo -f null -"
# print(command)

command_arr = [
    ffmpeg_exe, 
    "-i", 
    file_path, 
    "-vf", 
    "select='eq(n,31)',showinfo", 
    "-f", 
    "null", 
    "-"
]

# print('Getting frame type...')

proc = Popen(command, shell = True, stdin=PIPE, stdout=PIPE, stderr=STDOUT, close_fds=True)
result = str(proc.stdout.read())
# print('res: ', result)

key = ' type:'

# point = result.find(key) + len(key)
matches = [m.start() + len(key) for m in re.finditer(key, result)]

types = [result[point : point +1] for point in matches]

print(types)