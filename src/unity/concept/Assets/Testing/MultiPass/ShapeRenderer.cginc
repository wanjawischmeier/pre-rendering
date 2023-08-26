#ifndef SHAPE_RENDERER_CGINC
#define SHAPE_RENDERER_CGINC

// Based on: https://jstutorial.medium.com/how-to-code-your-first-algorithm-rdaw-a-line-ca121f9a1395
void DrawLine(int2 p0, int2 p1, float4 col)
{
    if (length(p0 - p1) > DEBUG_MAX_LINE_LENGTH)
    {
        return;
    }

    // Iterators, counters required by algorithm
    int x, y, dx, dy, dx1, dy1, px, py, xe, ye, i;

    // Calculate line deltas
    dx = p1.x - p0.x;
    dy = p1.y - p0.y;

    // Create a positive copy of deltas (makes iterating easier)
    dx1 = abs(dx);
    dy1 = abs(dy);

    // Calculate error intervals for both axis
    px = 2 * dy1 - dx1;
    py = 2 * dx1 - dy1;

    // The line is X-axis dominant
    if (dy1 <= dx1)
    {
        // Line is rdawn left to right
        if (dx >= 0)
        {
            x = p0.x; y = p0.y; xe = p1.x;
        }
        else
        {
            // Line is rdawn right to left (swap ends)
            x = p1.x; y = p1.y; xe = p0.x;
        }

        // Rasterize the line
        for (int i = 0; x < xe; i++) {
            x = x + 1;

            // Deal with octants...
            if (px < 0)
            {
                px = px + 2 * dy1;
            }
            else
            {
                if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0))
                {
                    y = y + 1;
                }
                else
                {
                    y = y - 1;
                }

                px = px + 2 * (dy1 - dx1);
            }

            // Draw pixel from line span at
            // currently rasterized position
            Rasterized[uint2(x, y)] = col;
        }
    }
    else
    {
        // The line is Y-axis dominant
        // Line is rdawn bottom to top
        if (dy >= 0)
        {
            x = p0.x; y = p0.y; ye = p1.y;
        }
        else
        {
            // Line is rdawn top to bottom
            x = p1.x; y = p1.y; ye = p0.y;
        }

        // Prevent unrolling of loop
        if (y == ye)
            return;

        // Rasterize the line
        for (int i = 0; y < ye; i++)
        {
            y = y + 1;

            // Deal with octants...
            if (py <= 0)
            {
                py = py + 2 * dx1;
            }
            else
            {
                if ((dx < 0 && dy < 0) || (dx > 0 && dy > 0))
                {
                    x = x + 1;
                }
                else
                {
                    x = x - 1;
                }

                py = py + 2 * (dx1 - dy1);
            }

            // Draw pixel from line span at
            // currently rasterized position
            Rasterized[uint2(x, y)] = col;
        }
    }
}

void DrawCircle(int2 p, float4 col)
{
    int x, y, px, nx, py, ny, d;

#if DEBUG_CIRCLE_RADIUS == 1
    Rasterized[int2(p.x, p.y)] += col;
#else
    [unroll(DEBUG_CIRCLE_RADIUS)]
    for (x = 0; x <= DEBUG_CIRCLE_RADIUS; x++)
    {
        d = (int)ceil(sqrt(DEBUG_CIRCLE_RAD_POW - x * x));
        for (y = 0; y <= d; y++)
        {
            px = p.x + x;
            nx = p.x - x;
            py = p.y + y;
            ny = p.y - y;

            Rasterized[int2(px, py)] += col;
            Rasterized[int2(nx, py)] += col;
            Rasterized[int2(px, ny)] += col;
            Rasterized[int2(nx, ny)] += col;
        }
    }
#endif
}

#endif // SHAPE_RENDERER_CGINC