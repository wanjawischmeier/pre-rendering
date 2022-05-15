from shader_emulator.shader_emulator import *
from os.path import join

def uv_test(id: float2, resolution: int2):
    write_texture("result", id, float4(
        id.x / float(resolution.x),
        id.y / float(resolution.y),
        0, 1
    ))

def projection(id: int2, resolution: int2) -> None:
    ll1 = float2(id.y, id.x) / float2(resolution.y, resolution.x) * float2(pi, pi2)
    ll1.y += pi

    cp = sample_texture("input", id).w * (fclip - nclip) + nclip

    p = float3(
        cp * sin(ll1.y) * sin(ll1.x),
        cp * cos(ll1.x),
        cp * cos(ll1.y) * sin(ll1.x)
    )

    p += position

    d = sqrt(p.x * p.x + p.y * p.y + p.z * p.z)

    ll2 = float2(
        acos(p.y / d),
        atan2(p.x, p.z)
    )

    a = float2(
        ll2.y / pi2,
        ll2.x / pi
    )
    a.x += 0.5
    
    idx = (a * resolution).round

    col = sample_texture("input", id)
    write_texture("result", idx, col)

resolution = int2(178, 50)
upscaled = int2(1000, 1000)
nclip = 2
fclip = 4
position = float3(-1, 0, -1)
path = "src\\python-testing\\optimal-projection"
image_file = "left_50p.png"
full_path = join(path, image_file)

load_texture("input", full_path)
create_texture("result", resolution, debug=True)
dispatch(uv_test, resolution, log=True)
show_texture("result", resolution=upscaled)