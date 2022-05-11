from math import sqrt


class Vector2:
    def __init__(self, x, y) -> None:
        self.x = x
        self.y = y

    @property
    def yx(self):
        return Vector2(self.y, self.x)

    @staticmethod
    def multiply(a, b):
        return Vector2(
            a.x * b.x,
            a.y * b.y
        )

    def divide(a, b):
        return Vector2(
            a.x / b.x,
            a.y / b.y
        )

    @staticmethod
    def round(vec):
        return Vector2(round(vec.x), round(vec.y))

    @staticmethod
    def magnitude(a) -> float:
        return sqrt(
            a.x**2 +
            a.y**2
        )
        

class Vector3:
    def __init__(self, x, y, z) -> None:
        self.x = x
        self.y = y
        self.z = z

    @staticmethod
    def add(a, b):
        return Vector3(
            a.x + b.x,
            a.y + b.y,
            a.z + b.z
        )

    @staticmethod
    def multiply(a, b):
        return Vector3(
            a.x * b.x,
            a.y * b.y,
            a.z * b.z
        )

    @staticmethod
    def divide(a, b):
        return Vector3(
            a.x / b.x,
            a.y / b.y,
            a.z / b.z
        )

    @staticmethod
    def magnitude(a) -> float:
        return sqrt(
            a.x**2 +
            a.y**2 +
            a.z**2
        )

a = Vector2(1, 2)
b = Vector2(5, 6)