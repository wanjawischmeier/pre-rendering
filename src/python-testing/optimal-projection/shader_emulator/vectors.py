class int2:
    def __init__(self, x: int, y: int):
        self.x = round(x)
        self.y = round(y)

    @property
    def as_tuple(self) -> tuple[int, int]:
        return (self.x, self.y)

class int4:
    def __init__(self, x: int, y: int, z: int, w: int):
        self.x = round(x)
        self.y = round(y)
        self.z = round(z)
        self.w = round(w)

    @property
    def as_tuple(self) -> tuple[int, int, int, int]:
        return (self.x, self.y, self.z, self.w)

    def normalize(self, max_value: int):
        floating_max = float(max_value)

        return float4(
            self.x / floating_max,
            self.y / floating_max,
            self.z / floating_max,
            self.w / floating_max
        )

class float2:
    def __init__(self, x: float, y: float):
        self.x = float(x)
        self.y = float(y)
    
    @property
    def yx(self):
        return float2(
            self.y,
            self.x
        )

    @property
    def as_tuple(self) -> tuple[float, float]:
        return (self.x, self.y)

    @property
    def round(self):
        return int2(
            self.x,
            self.y
        )

    def __mul__(self, other):
        return float2(
            self.x * other.x,
            self.y * other.y
        )

    def __truediv__(self, other):
        return float2(
            self.x / other.x,
            self.y / other.y
        )

class float3:
    def __init__(self, x: float, y: float, z: float):
        self.x = float(x)
        self.y = float(y)
        self.z = float(z)

    def __add__(self, other):
        return float3(
            self.x + other.x,
            self.y + other.y,
            self.z + other.z
        )

class float4:
    def __init__(self, x: float, y: float, z: float, w: float):
        self.x = float(x)
        self.y = float(y)
        self.z = float(z)
        self.w = float(w)

    @property
    def as_tuple(self) -> tuple[float, float, float, float]:
        return (self.x, self.y, self.z, self.w)

    @property
    def rgb2bgr(self):
        return float4(
            self.z,
            self.y,
            self.x,
            self.w
        )

    def rescale(self, max_value: int) -> int4:
        return int4(
            self.x * max_value,
            self.y * max_value,
            self.z * max_value,
            self.w * max_value
        )