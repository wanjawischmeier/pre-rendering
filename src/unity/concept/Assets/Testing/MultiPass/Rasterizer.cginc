#ifdef DEBUG
#include "ShapeRenderer.cginc"
#endif


void SortTriangleVerticiesByHeight(inout Triangle tri)
{
    ProjectedPoint tmp;

    // Sort by height (ascending)
    if (tri.v0.pc.y > tri.v1.pc.y)
    {
        SWAP(tri.v0, tri.v1);
    }
    if (tri.v1.pc.y > tri.v2.pc.y)
    {
        SWAP(tri.v1, tri.v2);
    }
    if (tri.v0.pc.y > tri.v1.pc.y)
    {
        SWAP(tri.v0, tri.v1);
    }
}

float3 InterpolateTriangle(int x, int y, int2 v0, int2 v1, int2 v2)
{
    float AB = length(v0 - v1);
    float BC = length(v1 - v2);
    float CA = length(v2 - v0);
    
    if (AB + BC < CA + TRI_DEGENERATE_THRESHOLD ||
        BC + CA < AB + TRI_DEGENERATE_THRESHOLD ||
        CA + AB < BC + TRI_DEGENERATE_THRESHOLD)
    {
        // maybe differenciate between small and flat tris
        // and linearly interpolate the latter for a more accurate result
        return (1 / 3.0).xxx;
    }
    
    float dv = (v1.y - v2.y) * (v0.x - v2.x) + (v2.x - v1.x) * (v0.y - v2.y);
    float w0 = ((v1.y - v2.y) * (x - v2.x) + (v2.x - v1.x) * (y - v2.y)) / dv;
    float w1 = ((v2.y - v0.y) * (x - v2.x) + (v0.x - v2.x) * (y - v2.y)) / dv;
    float w2 = 1 - w0 - w1;
    
    return float3(w0, w1, w2);
}

void DrawRow(int x0, int y0, int x1, Triangle tri, Triangle unsorted)
{
    if (x1 - x0 <= 0)
        return;

    for (int i = x0; i <= x1; i++)
    {
        float3 w = InterpolateTriangle(i, y0, unsorted.v0.pc, unsorted.v1.pc, unsorted.v2.pc);
        float2 uv = unsorted.v0.oc * w.x + unsorted.v1.oc * w.y + unsorted.v2.oc * w.z;
        float d = tri.v0.d * w.x + tri.v1.d * w.y + tri.v2.d * w.z;
        
        uint2 tc = uint2(i, y0);
        float og = RasterizedDepth[tc];
                
        if (d < og || og == 0)
        {
            // offset by one to allow checking for unset pixels
            Rasterized[tc] = uv + 1;
            RasterizedDepth[tc] = d;
        }
        
        if (i - x0 > MAX_SCANLINE_LENGTH)
            break;
    }
}

// Based on: http://www.sunshine2k.de/coding/java/TriangleRasterization/TriangleRasterization.html
void RasterizeTopFlatTriangle(Triangle tri, Triangle unsorted)
{
    float invslope0 = (tri.v1.pc.x - tri.v0.pc.x) / (float)(tri.v1.pc.y - tri.v0.pc.y);
    float invslope1 = (tri.v2.pc.x - tri.v0.pc.x) / (float)(tri.v2.pc.y - tri.v0.pc.y);

    // ensure curx0 < curx1
    if (invslope1 < invslope0)
    {
        float tmp = invslope0;
        invslope0 = invslope1;
        invslope1 = tmp;
    }

    float curx0 = tri.v0.pc.x;
    float curx1 = tri.v0.pc.x;

    for (int scanlineY = tri.v0.pc.y; scanlineY <= tri.v1.pc.y; scanlineY++)
    {
        DrawRow(curx0, scanlineY, curx1, tri, unsorted);

        curx0 += invslope0;
        curx1 += invslope1;

        if (scanlineY - tri.v0.pc.y > DEBUG_MAX_LINE_LENGTH)
            break;
    }
}

// See comment above
void RasterizeBottomFlatTriangle(Triangle tri, Triangle unsorted)
{
    float invslope0 = (tri.v0.pc.x - tri.v1.pc.x) / (float)(tri.v0.pc.y - tri.v1.pc.y);
    float invslope1 = (tri.v0.pc.x - tri.v2.pc.x) / (float)(tri.v0.pc.y - tri.v2.pc.y);

    // ensure curx0 < curx1
    if (invslope1 > invslope0)
    {
        float tmp = invslope0;
        invslope0 = invslope1;
        invslope1 = tmp;
    }

    float curx0 = tri.v0.pc.x;
    float curx1 = tri.v0.pc.x;

    for (int scanlineY = tri.v0.pc.y; scanlineY > tri.v1.pc.y; scanlineY--)
    {
        DrawRow(curx0, scanlineY, curx1, tri, unsorted);

        curx0 -= invslope0;
        curx1 -= invslope1;

        if (tri.v0.pc.y - scanlineY > DEBUG_MAX_LINE_LENGTH)
            break;
    }
}

void RasterizeTriangle(Triangle tri)
{
    Triangle unsorted = tri;
    SortTriangleVerticiesByHeight(tri);

    // TODO: add screen border clipping

    // create a 4th vertex
    ProjectedPoint v3;
    v3.uv = float2(tri.v0.uv.x + ((tri.v1.uv.y - tri.v0.uv.y) / (tri.v2.uv.y - tri.v0.uv.y)) * (tri.v2.uv.x - tri.v0.uv.x), tri.v1.uv.y);
    v3.pc = MAP_TO_RANGE(v3.uv, RASTERIZATION_RESOLUTION);
    
    // calculate weight along line
    float l0 = length(tri.v0.uv - v3.uv);
    float l2 = length(tri.v0.uv - tri.v2.uv);
    float w0 = l0 / l2;
    float w2 = 1 - w0;

    // extrapolate data for 4th vertex
    v3.oc = tri.v0.oc * w0 + tri.v2.oc * w2;
    v3.d = tri.v0.d * w0 + tri.v2.d * w2;
    
#ifdef DEBUG
    // TODO: fix for normalized oc
    if (CURRENT_PASS == DEBUG_PASSES - 1 && DEBUG_MODE == DEBUG_MODE_HIGHLIGHT_POINT && tri.v0.oc.x == DEBUG_INT && tri.v0.oc.y == 4)
    {
        DrawCircle(tri.v0.pc, DEBUG_COL_POINT_0);
    }
#endif

    // split the triangle into 2
    Triangle top, btm; // top: (v0, v1, v3) | btm: (v2, v1, v3)
    top.v0 = tri.v0;
    btm.v0 = tri.v2;
    top.v1 = btm.v1 = tri.v1;
    top.v2 = btm.v2 = v3;

#ifdef DEBUG
    if (CURRENT_PASS != DEBUG_PASSES - 1 || (
        DEBUG_MODE != DEBUG_MODE_Z_SINE &&
        DEBUG_MODE != DEBUG_MODE_HIGHLIGHT_POINT &&
        DEBUG_MODE != DEBUG_MODE_POINT_CLOUD &&
        DEBUG_MODE != DEBUG_MODE_WIREFRAME))
    {
#endif
        RasterizeTopFlatTriangle(top, unsorted);
        RasterizeBottomFlatTriangle(btm, unsorted);
#ifdef DEBUG
    }
#endif

#ifdef DEBUG
    if (CURRENT_PASS == DEBUG_PASSES - 1 && (DEBUG_MODE == DEBUG_MODE_HIGHLIGHT_QUAD || DEBUG_MODE == DEBUG_MODE_WIREFRAME))
    {
        if (tri.v0.pc.y != tri.v1.pc.y || tri.v0.pc.y != tri.v2.pc.y)
        {
            DrawLine(tri.v0.pc, tri.v1.pc, DEBUG_COL_LINE);
            DrawLine(tri.v0.pc, tri.v2.pc, DEBUG_COL_LINE);
            DrawLine(tri.v1.pc, tri.v2.pc, DEBUG_COL_LINE);
            DrawLine(tri.v1.pc, v3.pc, DEBUG_COL_LINE / 4); // debug middle line lighter
        }
    }
#endif
}