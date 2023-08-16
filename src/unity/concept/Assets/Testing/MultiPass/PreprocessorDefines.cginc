// constants
#define QUAD_VERTEX_COUNT 4
#define MAX_SCANLINE_LENGTH 1000


// debug constants
#ifdef DEBUG
#define DEBUG_MODE_NONE 0
#define DEBUG_MODE_Z_SINE 1
#define DEBUG_MODE_HIGHLIGHT_POINT 2
#define DEBUG_MODE_HIGHLIGHT_QUAD 3
#define DEBUG_MODE_POINT_CLOUD 4
#define DEBUG_MODE_WIREFRAME 5

#define DEBUG_COL_LINE float2(1, 1)
#define DEBUG_COL_POINT_0 float2(1, 0)
#define DEBUG_COL_POINT_1 float2(1, 0.5)
#define DEBUG_COL_POINT_2 float2(0, 1)
#define DEBUG_COL_POINT_3 float2(0.5, 1)

#define DEBUG_MAX_LINE_LENGTH 600
#define DEBUG_CIRCLE_RADIUS 4
#define DEBUG_CIRCLE_RAD_POW (DEBUG_CIRCLE_RADIUS * DEBUG_CIRCLE_RADIUS)
#endif


// macros
#define MAP_TO_RANGE(tc, targetRange) \
    (tc) * ((targetRange) - 1)

#define NORMALIZE_RANGE(tc, sourceRange) \
    (tc) / ((sourceRange) - 1)

#define REMAP_TO_RANGE(tc, sourceRange, targetRange) \
    MAP_TO_RANGE(NORMALIZE_RANGE(tc, sourceRange), targetRange)

#define VALID_POINT(projectedPoint) \
    !(projectedPoint.uv.x == -1 || projectedPoint.uv.y == -1)

#define VALID_TRIANGLE_POINTS(tri) \
    VALID_POINT(tri.v0) + VALID_POINT(tri.v1) + VALID_POINT(tri.v2)

// requires declaration of a tmp variable before invocation
#define SWAP(a, b) \
    tmp = a; \
    a = b; \
    b = tmp;


// texture switching optimization
#ifdef PASS_0
#define Projected Projected0
#endif

#ifdef PASS_1
#define Projected Projected1
#endif

#ifdef PASS_2
#define Projected Projected2
#endif

#ifdef PASS_3
#define Projected Projected3
#endif