#define MAX_SLICES 2
#define MAX_INT_CONST 9999
#define VERTICIES_PER_QUAD 6
#define DEPTH_TOLERANCE 0.0001
#define VALIDATION_ITERATIONS 0
#define CUBEMAP_FACE_COUNT 6
#define CUBEMAP_SCALE 5 // TODO: equivalent to FCLIP?


#define MAP_TO_RANGE(tc, targetRange)                                       \
    (tc) * ((targetRange) - 1)

#define NORMALIZE_RANGE(tc, sourceRange)                                    \
    (tc) / ((sourceRange) - 1)

#define MARK_VERTEX_INVALID(tc, index)                                      \
    _MotionVectorsWrite[uint3(tc, index)] = float4(0, 0, 0, 0);             \
    return;

#define RETURN_INVALID_VERTEX()                                             \
    o.pos = float4(0, 0, 0, 0);                                             \
    o.uv = float4(0, 0, 0, -1);                                             \
    o.depth = 0;                                                            \
    return o;

#define MIX_COL_UI_BY_ALPHA(col, ui)                                        \
    UI_DEBUGGER == 1 ? col * (1 - ui.g) + ui.rrrr * ui.g : col

// propably violating the genova convention
// the camera refused to render onto multiple slices, so this has to exist :/
#define READ_PSEUDO_ARRAY(array, tc, slice, result)                         \
    switch (slice) {                                                        \
        case 0:                                                             \
            result = array##0[tc];                                          \
            break;                                                          \
        case 1:                                                             \
            result = array##1[tc];                                          \
            break;                                                          \
        case 2:                                                             \
            result = array##2[tc];                                          \
            break;                                                          \
        case 3:                                                             \
            result = array##3[tc];                                          \
            break;                                                          \
        case 4:                                                             \
            result = array##4[tc];                                          \
            break;                                                          \
        case 5:                                                             \
            result = array##5[tc];                                          \
            break;                                                          \
        case 6:                                                             \
            result = array##6[tc];                                          \
            break;                                                          \
        case 7:                                                             \
            result = array##7[tc];                                          \
            break;                                                          \
        default:                                                            \
            result = float4(1, 0, 1, 1);                                    \
            break;                                                          \
    }

// find it in your heart to forgive the universe for the existence of this macro
// the compiler cannot map Texture2D.Sample to the vs_4_0 instruction set for some reason
#define SAMPLE_PSEUDO_ARRAY(array, uv, slice, result)                       \
    switch (slice) {                                                        \
        case 0:                                                             \
            result = array##0.SampleLevel(sampler_linear_repeat, uv, 0);    \
            break;                                                          \
        case 1:                                                             \
            result = array##1.SampleLevel(sampler_linear_repeat, uv, 0);    \
            break;                                                          \
        case 2:                                                             \
            result = array##2.SampleLevel(sampler_linear_repeat, uv, 0);    \
            break;                                                          \
        case 3:                                                             \
            result = array##3.SampleLevel(sampler_linear_repeat, uv, 0);    \
            break;                                                          \
        case 4:                                                             \
            result = array##4.SampleLevel(sampler_linear_repeat, uv, 0);    \
            break;                                                          \
        case 5:                                                             \
            result = array##5.SampleLevel(sampler_linear_repeat, uv, 0);    \
            break;                                                          \
        case 6:                                                             \
            result = array##6.SampleLevel(sampler_linear_repeat, uv, 0);    \
            break;                                                          \
        case 7:                                                             \
            result = array##7.SampleLevel(sampler_linear_repeat, uv, 0);    \
            break;                                                          \
        default:                                                            \
            result = float4(1, 0, 1, 1);                                    \
            break;                                                          \
    }