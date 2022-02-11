from math import pi
from manim import *

class ProjectionDemo(Scene):
    def construct(self):
        
        square1 = Square()
        self.play(Create(square1))
        self.play(
            square1.animate
                .set_opacity(1)
                .set_color(ORANGE)
                .move_to((2, 1, 0))
                .scale(0.6)
        )

        square2 = Square()
        self.play(Create(square2))
        self.play(
            square2.animate
                .set_opacity(1)
                .set_color(BLUE)
                .move_to((-0, -2, 0))
                .rotate(pi / 12)
                .scale(0.8)
        )

        triangle = Triangle(color=WHITE)
        self.play(Create(triangle))
        self.play(
            triangle.animate
                .set_opacity(1)
                .set_color(GREEN)
                .move_to((-1.8, 1.2, 0))
                .rotate(pi / 3)
                .scale(0.8)
        )
        
        circle = Circle(3.5, color=DARKER_GREY)
        circle.stroke_width = 8
        circle.rotate(90*DEGREES)
        self.play(Create(circle))
        
        def magnitude(v: tuple) -> float:
            return v[0]**2+v[1]**2+v[2]**2

        def intersection_distance(v: tuple[tuple,tuple]) -> float:
            return magnitude(v[0])
        
        def update_ray(obj: Mobject):
            intersections = [
                (Intersection(irect, square1), square1.get_color()),
                (Intersection(irect, square2), square2.get_color()),
                (Intersection(irect, triangle), triangle.get_color())
            ]

            points = []
            for intersection, color in intersections:
                for point in intersection.points:
                    points.append((point, color))

            point_on_circle = normalize(irect.get_end()) * circle.radius

            if (len(points) != 0):
                closest, color = sorted(points, key=intersection_distance)[0]

                if use_depth:
                    depth = 1 - min(1, max(0, magnitude(closest) / circle.radius * 4 - 2))
                    color = rgb_to_color((depth, depth, depth))

                new_line = Line(ORIGIN, closest)

                inner_dot = Dot(closest, DEFAULT_SMALL_DOT_RADIUS)
                outer_dot = Dot(point_on_circle, DEFAULT_SMALL_DOT_RADIUS, color=color)
                dots.add(inner_dot, outer_dot)
                self.add(inner_dot, outer_dot)
            else:
                new_line = Line(ORIGIN, point_on_circle)
            
            line.become(new_line)

        angle = -pi / 2
        circle_time = 2
        use_depth = False
        rates = [
            rate_functions.ease_in_sine,
            rate_functions.linear,
            rate_functions.linear,
            rate_functions.ease_out_sine
        ]
        dots = VGroup()
        self.add(dots)
        irect = Rectangle(height=0.01, width=circle.radius)
        irect.stroke_width = 0
        irect.move_to((circle.radius/2, 0, 0))
        irect.rotate_about_origin(pi / 2)
        irect.add_updater(update_ray)
        
        line = Line(ORIGIN, UP * circle.radius)
        self.play(Create(irect), Create(line))
        self.wait(2)
        
        for i in range(4):
            self.play(
                irect.animate.rotate_about_origin(angle),
                run_time=circle_time,
                rate_func=rates[i]
            )

        use_depth = True
        self.play(FadeOut(dots), lag_ratio=2)
        dots.reset_points()
        self.wait(2)
        
        for i in range(4):
            self.play(
                irect.animate.rotate_about_origin(angle),
                run_time=circle_time,
                rate_func=rates[i]
            )

        self.wait(2)
        
        open_angle = 120
        offset = 90
        arc = Arc(circle.radius, (open_angle/2+offset)*DEGREES, (360-open_angle)*DEGREES)
        line = Line(LEFT*4, RIGHT*4)
        # self.play(FadeOut(dots), lag_ratio=2)
        # self.remove(dots)
        # self.play(Uncreate(square1, square2, triangle, line))
        self.play(ReplacementTransform(circle, arc))
        self.play(ReplacementTransform(arc, line))
        self.play(line.animate.rotate(90*DEGREES).scale(0.5).shift(4*RIGHT))
        
        # self.wait(2)

        rect = Rectangle(height=4, width=0.001)
        rect.move_to(4*RIGHT)
        self.add(rect)
        self.remove(line)
        self.play(rect.animate.stretch_to_fit_width(8).move_to(ORIGIN))

        plane = NumberPlane((0, 16), (0, 8), 8, 4)
        self.play(Create(plane), run_time=2)
        self.play(Uncreate(plane))
        
        line.move_to(4*LEFT)
        line.rotate(180*DEGREES)
        self.play(rect.animate.stretch_to_fit_width(0.001).move_to(4*LEFT))
        self.add(line)
        self.remove(rect)

        arc = Arc(circle.radius, open_angle/2*DEGREES, (360-open_angle)*DEGREES)
        circle = Circle(3.5, color=DARKER_GREY)
        circle.stroke_width = 8
        
        self.play(ReplacementTransform(line, arc))
        self.play(ReplacementTransform(arc, circle))

        self.wait(1)