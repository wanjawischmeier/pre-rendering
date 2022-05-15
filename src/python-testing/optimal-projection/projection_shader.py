from shader_emulator import *

def projection(id: float2, width, height) -> None:
    write_texture("result", id, float4(id.x / float(width), id.y / float(height), 0, 1))


resolution = (100, 100)
create_texture("result", resolution, debug=True)
dispatch(projection, resolution, log=True)
show_texture("result", resolution=(1000, 1000))