import ffmpeg
import numpy as np
import os
from plugin import variables, expressions

path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\renders\\cycles\\room_simple_v2_720p"
width = 5120
height = 1440
frames = 441

process1 = (
    ffmpeg
    .input(f"{path}\\%04d.png")
    .filter('fps', fps=30, round='up')
    .output("pipe:", format="rawvideo", pix_fmt="rgb24")
    .run_async(pipe_stdout=True)
)

process2 = (
    ffmpeg
    .input("pipe:", format="rawvideo", pix_fmt="rgb24", s=f"{width}x{height}")
    .output(f"{path}\\output.mp4", pix_fmt="yuv420p")
    .overwrite_output()
    .run_async(pipe_stdin=True)
)

for i in range(frames):
    in_bytes = process1.stdout.read(width * height * 3)
    if not in_bytes:
        break
    in_frame = (
        np
        .frombuffer(in_bytes, np.uint8)
        .reshape([height, width, 3])
    )
    out_frame = in_frame
    process2.stdin.write(
        out_frame
        .astype(np.uint8)
        .tobytes()
    )

process2.stdin.close()
process1.wait()
process2.wait()