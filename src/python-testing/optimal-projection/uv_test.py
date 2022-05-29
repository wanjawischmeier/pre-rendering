from shader_emulator.shader_emulator import *

def uv_test(id: float2, resolution: int2):
    write_texture("result", id, float4(
        id.x / float(resolution.x),
        id.y / float(resolution.y),
        0, 1
    ))


upscaled = int2(400, 400)

resolution = int2(10, 10)
create_texture("result", resolution)
dispatch(uv_test, resolution)
show_texture("result", resolution=upscaled)