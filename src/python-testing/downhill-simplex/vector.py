from math import sqrt


class float2:
    def __init__(self, x: float, y: float) -> None:
        self.x = x
        self.y = y

    def __add__(self, other):
        return float2(self.x + other.x, self.y + other.y)

    def __sub__(self, other):
        return float2(self.x - other.x, self.y - other.y)

    def __rmul__(self, other):
        if type(other) == float2: 
            return float2(self.x * other.x, self.y * other.y)
        else: 
            return float2(self.x * other, self.y * other)

    def __truediv__(self, other):
        if type(other) == float2: 
            return float2(self.x / other.x, self.y / other.y)
        else: 
            return float2(self.x / other, self.y / other)
    
    @staticmethod
    def magnitude(vector) -> float:
        return sqrt(vector.x * vector.x + vector.y * vector.y)
    
    @staticmethod
    def round(vector):
        return float2(round(vector.x), round(vector.y))
    
    @staticmethod
    def as_tuple(vector) -> tuple[float, float]:
        return (vector.x, vector.y)



class float3:
    def __init__(self, x: float, y: float, z: float) -> None:
        self.x = x
        self.y = y
        self.z = z

    def __add__(self, other):
        return float3(self.x + other.x, self.y + other.y, self.z)

    def __sub__(self, other):
        return float3(self.x - other.x, self.y - other.y, self.z)

    def __rmul__(self, other):
        if type(other) == float3: 
            return float3(self.x * other.x, self.y * other.y, self.z)
        else: 
            return float3(self.x * other, self.y * other, self.z)

    def __truediv__(self, other):
        if type(other) == float3: 
            return float3(self.x / other.x, self.y / other.y, self.z)
        else: 
            return float3(self.x / other, self.y / other, self.z)
    
    @staticmethod
    def magnitude(vector):
        return sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z)
    
    @staticmethod
    def round(vector):
        return float3(round(vector.x), round(vector.y), vector.z)

    @staticmethod
    def expand_float2(xy: float2, z: float):
        return float3(xy.x, xy.y, z)