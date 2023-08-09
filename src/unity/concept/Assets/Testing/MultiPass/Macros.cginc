#define NCLIP 1
#define FCLIP 30
#define QUAD_VERTEX_COUNT 4
#define DEBUG_BLUE float4(0.2, 0.2, 1, 1)
#define DEBUG_MAX_LINE_LENGTH 600
#define DEBUG_CIRCLE_RADIUS 4
#define DEBUG_CIRCLE_RAD_POW (DEBUG_CIRCLE_RADIUS * DEBUG_CIRCLE_RADIUS)

#define MAP_TO_RANGE(tc, targetRange) \
    (tc) * ((targetRange) - 1)
#define NORMALIZE_RANGE(tc, sourceRange) \
    (tc) / ((sourceRange) - 1)
#define REMAP_TO_RANGE(tc, sourceRange, targetRange) \
    MAP_TO_RANGE(NORMALIZE_RANGE(tc, sourceRange), targetRange)
#define VALID_POINT(projectedPoint) \
    !(projectedPoint.uv.x == -1 || projectedPoint.uv.y == -1)
#define VALID_TRIANGLE(tri) \
    VALID_POINT(tri.v0) + VALID_POINT(tri.v1) + VALID_POINT(tri.v2) > 1