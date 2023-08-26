// constants
#define QUAD_VERTEX_COUNT 4
#define MAX_SCANLINE_LENGTH 100
#define PROJECTED_EMPTY_POINT float4(-1, -1, -1, -1)
#define INTERPOLATION_SEARCH_RADIUS 8
#define TRI_DEGENERATE_THRESHOLD 1


// debug constants
#ifdef DEBUG
#define DEBUG_MODE_NONE 0
#define DEBUG_MODE_Z_SINE 1
#define DEBUG_MODE_HIGHLIGHT_POINT 2
#define DEBUG_MODE_HIGHLIGHT_QUAD 3
#define DEBUG_MODE_POINT_CLOUD 4
#define DEBUG_MODE_WIREFRAME 5
#define DEBUG_MODE_Z_SINE_FILLED 6

#define DEBUG_COL_LINE float4(1, 1, 1, 1)
#define DEBUG_COL_POINT_0 float4(1, 0, 0, 1)
#define DEBUG_COL_POINT_1 float4(1, 0.5, 0, 1)
#define DEBUG_COL_POINT_2 float4(0, 1, 0, 1)
#define DEBUG_COL_POINT_3 float4(0.5, 1, 0, 1)

#define DEBUG_MAX_LINE_LENGTH 600
#define DEBUG_CIRCLE_RADIUS 1
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
    (projectedPoint.uv.x != -1)

#define VALID_TRIANGLE_POINTS(tri) \
    VALID_POINT(tri.v0) + VALID_POINT(tri.v1) + VALID_POINT(tri.v2)

// requires declaration of a tmp variable before invocation
#define SWAP(a, b) \
    tmp = a; \
    a = b; \
    b = tmp;


// texture switching optimization
#ifdef PASS_0
#define CURRENT_PASS 0
#endif

#ifdef PASS_1
#define CURRENT_PASS 1
#define LAST_PASS 0
#endif

#ifdef PASS_2
#define CURRENT_PASS 2
#define LAST_PASS 1
#endif

#ifdef PASS_3
#define CURRENT_PASS 3
#define LAST_PASS 2
#endif

// helper macro for concatenation
#define CONCAT(a, b) a##b

#define Projected CONCAT(Projected_, CURRENT_PASS)
#define ProjectedDepth CONCAT(ProjectedDepth_, CURRENT_PASS)
#define Rasterized CONCAT(Rasterized_, CURRENT_PASS)
#define RasterizedDepth CONCAT(RasterizedDepth_, CURRENT_PASS)

#ifdef LAST_PASS
#define PreviousPass CONCAT(Rasterized_, LAST_PASS)
#endif