// constants
#define NCLIP 1
#define FCLIP 30
#define QUAD_VERTEX_COUNT 4


// debug constants
#ifdef DEBUG
#define DEBUG_MODE_NONE 0
#define DEBUG_MODE_Z_SINE 1
#define DEBUG_MODE_HIGHLIGHT_POINT 2
#define DEBUG_MODE_HIGHLIGHT_QUAD 3
#define DEBUG_MODE_POINT_CLOUD 4
#define DEBUG_MODE_WIREFRAME 5

#define DEBUG_COL_LINE float4(1, 1, 1, 1)
#define DEBUG_COL_POINT_0 float4(0.2, 0.2, 1, 1)
#define DEBUG_COL_POINT_1 DEBUG_COL_POINT_0.bbga
#define DEBUG_COL_POINT_2 DEBUG_COL_POINT_0.gbga
#define DEBUG_COL_POINT_3 DEBUG_COL_POINT_0.bgga

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

// used to only render tris that are onscreen
#define VALID_TRIANGLE(tri) \
    VALID_POINT(tri.v0) || VALID_POINT(tri.v1) || VALID_POINT(tri.v2)


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