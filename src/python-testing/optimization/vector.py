from math import sqrt
from random import random


class float2:
    def __init__(self, x: float, y: float) -> None:
        self.x = x
        self.y = y

    def __add__(self, other):
        if type(other) == float2:
            return float2(self.x + other.x, self.y + other.y)
        else:
            return float2(self.x + other, self.y + other)

    def __sub__(self, other):
        if type(other) == float2:
            return float2(self.x - other.x, self.y - other.y)
        else:
            return float2(self.x - other, self.y - other)

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

    def __mod__(self, other):
        if type(other) == float2:
            return float2(self.x % other.x, self.y % other.y)
        else:
            return float2(self.x % other, self.y % other)
    
    @staticmethod
    def magnitude(vector) -> float:
        return sqrt(vector.x * vector.x + vector.y * vector.y)
    
    @staticmethod
    def round(vector):
        return float2(round(vector.x), round(vector.y))
    
    @staticmethod
    def as_tuple(vector) -> tuple[float, float]:
        return (vector.x, vector.y)
    
    @staticmethod
    def as_tuple_tc(vector, resolution) -> tuple[float, float]:
        return float2.as_tuple(float2.round(vector.__rmul__(resolution)))
    
    @staticmethod
    def random():
        return float2(random(), random())



class float3:
    def __init__(self, x: float, y: float, z: float) -> None:
        self.x = x
        self.y = y
        self.z = z

    def __add__(self, other):
        return float3(self.x + other.x, self.y + other.y, self.z + other.x)

    def __sub__(self, other):
        return float3(self.x - other.x, self.y - other.y, self.z - other.z)

    def __rmul__(self, other):
        if type(other) == float3: 
            return float3(self.x * other.x, self.y * other.y, self.z)
        else: 
            return float3(self.x * other, self.y * other, self.z)

    def __truediv__(self, other):
        if type(other) == float3: 
            return float3(self.x / other.x, self.y / other.y, self.z)
        else: 
            return float3(self.x / other, self.y / other, self.z / other)
    
    @staticmethod
    def magnitude(vector):
        return sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z)
    
    @staticmethod
    def round(vector):
        return float3(round(vector.x), round(vector.y), vector.z)

    @staticmethod
    def abs(vector):
        return float3(abs(vector.x), abs(vector.y), vector.z)

    @staticmethod
    def expand_float2(xy: float2, z: float):
        return float3(xy.x, xy.y, z)
    
    @staticmethod
    def as_tuple(vector) -> tuple[float, float, float]:
        return (vector.x, vector.y, vector.z)

    @property
    def xy(self):
        return float2(self.x, self.y)