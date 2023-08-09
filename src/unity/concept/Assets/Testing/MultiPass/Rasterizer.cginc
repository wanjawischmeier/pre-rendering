#if WIREFRAME
#include "ShapeRenderer.cginc"
#endif


float3 InterpolateTriangle(int x, int y, int2 v0, int2 v1, int2 v2)
{
    float dv = (v1.y - v2.y) * (v0.x - v2.x) + (v2.x - v1.x) * (v0.y - v2.y);
    float w0 = ((v1.y - v2.y) * (x - v2.x) + (v2.x - v1.x) * (y - v2.y)) / dv;
    float w1 = ((v2.y - v0.y) * (x - v2.x) + (v0.x - v2.x) * (y - v2.y)) / dv;
    float w2 = 1 - w0 - w1;

    return float3(w0, w1, w2);
}

void DrawRow(int x0, int y0, int x1, Triangle tri)
{
    int sx, ex;
    sx = x0;
    ex = x1;

    if (ex - sx <= 0)
        return;

    for (int i = sx; i <= ex; i++)
    {
        float3 w = InterpolateTriangle(i, y0, tri.v0.pc, tri.v1.pc, tri.v2.pc);
        int2 pc = tri.v0.oc * w.x + tri.v1.oc * w.y + tri.v2.oc * w.z;
        float2 uv = NORMALIZE_RANGE(pc, PROJECTION_RESOLUTION);
        float d = tri.v0.d * w.x + tri.v1.d * w.y + tri.v2.d * w.z;
        uint2 tc = uint2(i, y0);
        float og = RasterizedDepth[tc];

        if (d < og || og == 0)
        {
            RasterizedDepth[tc] = w.x;
            // Rasterized[tc] = Input[pc];
            Rasterized[tc] = Input[MAP_TO_RANGE(uv, INPUT_RESOLUTION)];
            // Rasterized[tc] = float4(NORMALIZE_RANGE(tri.v0.oc, PROJECTION_RESOLUTION), 0, 1);
        }

        if (i - sx > 200)
            break;
    }
}

// Based on: http://www.sunshine2k.de/coding/java/TriangleRasterization/TriangleRasterization.html
void RasterizeTopFlatTriangle(Triangle tri)
{
    if (tri.v0.pc.y >= tri.v1.pc.y)
        return;
    if (!any(tri.v0.oc))
        return;

    float invslope1 = (tri.v1.pc.x - tri.v0.pc.x) / (float)(tri.v1.pc.y - tri.v0.pc.y);
    float invslope2 = (tri.v2.pc.x - tri.v0.pc.x) / (float)(tri.v2.pc.y - tri.v0.pc.y);

    float curx1 = tri.v0.pc.x;
    float curx2 = tri.v0.pc.x;

    for (int scanlineY = tri.v0.pc.y; scanlineY <= tri.v1.pc.y; scanlineY++)
    {
        DrawRow(curx1, scanlineY, curx2, tri);

        curx1 += invslope1;
        curx2 += invslope2;

        if (scanlineY - tri.v0.pc.y > DEBUG_MAX_LINE_LENGTH)
            break;
    }
}

// See comment above
void RasterizeBottomFlatTriangle(Triangle tri)
{
    if (tri.v0.pc.y <= tri.v1.pc.y)
        return;
    if (!any(tri.v0.oc))
        return;

    float invslope1 = (tri.v0.pc.x - tri.v1.pc.x) / (float)(tri.v0.pc.y - tri.v1.pc.y);
    float invslope2 = (tri.v0.pc.x - tri.v2.pc.x) / (float)(tri.v0.pc.y - tri.v2.pc.y);

    float curx1 = tri.v0.pc.x;
    float curx2 = tri.v0.pc.x;

    for (int scanlineY = tri.v0.pc.y; scanlineY > tri.v1.pc.y; scanlineY--)
    {
        DrawRow(curx1, scanlineY, curx2, tri);

        curx1 -= invslope1;
        curx2 -= invslope2;

        if (tri.v0.pc.y - scanlineY > DEBUG_MAX_LINE_LENGTH)
            break;
    }
}

void RasterizeTriangle(Triangle tri)
{
    if (tri.v0.pc.x < 0 || tri.v1.pc.y < 0)
    {
        return;
    }

    // create a 4th vertex
    ProjectedPoint v3;
    v3.uv = float2(tri.v0.uv.x + ((tri.v1.uv.y - tri.v0.uv.y) / (tri.v2.uv.y - tri.v0.uv.y)) * (tri.v2.uv.x - tri.v0.uv.x), tri.v1.uv.y);
    v3.pc = MAP_TO_RANGE(v3.uv, RASTERIZATION_RESOLUTION);
    // initialize variables to pass as parameter
    v3.oc = int2(-1, -1);
    v3.d = -1;

    /*
    float d0 = Input[tri.v0.oc].a;
    float d1 = Input[tri.v1.oc].a;
    float3 P0 = uvToVector(NORMALIZE_RANGE(tri.v0.oc, PROJECTION_RESOLUTION), d0);
    float3 P1 = uvToVector(NORMALIZE_RANGE(tri.v1.oc, PROJECTION_RESOLUTION), d1);
    float3 relativeDirection = P1 - P0;
    float2 textureDifference = tri.v1.pc - tri.v0.pc;
    int2 diff0 = abs(REMAP_TO_RANGE(tri.v0.oc, PROJECTION_RESOLUTION, RASTERIZATION_RESOLUTION) - tri.v0.pc);
    int2 diff1 = abs(REMAP_TO_RANGE(tri.v1.oc, PROJECTION_RESOLUTION, RASTERIZATION_RESOLUTION) - tri.v1.pc);
    */
#if WIREFRAME
    if (tri.v0.oc.x == DEBUG_INT && tri.v0.oc.y == 4)
    {
        DrawCircle(tri.v0.pc, DEBUG_BLUE);
        // DrawCircle(tri.v1.pc, tri.v0.oc.x < tri.v1.oc.x ? DEBUG_BLUE.gbra : DEBUG_BLUE.bgra);
        // DrawCircle(tri.v1.pc, abs(diff0.x - diff1.x) < 300 ? DEBUG_BLUE.gbra : DEBUG_BLUE.bgra);
    }
#endif

    // WrapProjectedPointsInRange(tri.v0, tri.v1);
    // WrapProjectedPointsInRange(tri.v0, tri.v2);
    // WrapProjectedPointsInRange(tri.v1, tri.v2);
    // WrapProjectedPointsInRange(tri.v1, v3);

    float l0 = length(tri.v0.uv - v3.uv);
    float l2 = length(tri.v2.uv - v3.uv);
    float w0 = l0 / (l0 + l2);
    float w2 = 1 - w0;

    v3.oc = tri.v0.oc * w0 + tri.v2.oc * w2;
    v3.d = tri.v0.d * w0 + tri.v2.d * w2;

    // split the triangle in 2
    Triangle top, btm;
    top.v0 = tri.v0;
    btm.v0 = tri.v2;
    top.v1 = btm.v1 = tri.v1;
    top.v2 = btm.v2 = v3;
    /*
    if (!any(top.v0.oc))
        return;
    */
    // RasterizeTopFlatTriangle(top);
    // RasterizeBottomFlatTriangle(btm);

    if (!(VALID_POINT(tri.v0) && VALID_POINT(tri.v1)))
    {
        return;
    }

#if WIREFRAME
    if (tri.v0.pc.y != tri.v1.pc.y || tri.v0.pc.y != tri.v2.pc.y)
    {
        // DrawLine(tri.v0.pc, tri.v1.pc, DEBUG_BLUE.bbba);
        // DrawLine(tri.v0.pc, tri.v2.pc, DEBUG_BLUE.bbba);
        // DrawLine(tri.v1.pc, tri.v2.pc, DEBUG_BLUE.bbba);
        // DrawLine(tri.v1.pc, v3.pc, DEBUG_BLUE.bbba);
    }
#endif
}