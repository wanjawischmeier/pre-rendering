struct ProjectedPoint
{
    // projected uv coordinates
    float2 uv;

    // absolute projected coordinates
    int2 pc;

    // corresponding original coordinates
    int2 oc;

    // projected dpeth value (non-normalized)
    float d;
};

struct Triangle
{
    // vertex points ascending by height
    ProjectedPoint v0, v1, v2;
};

float QuadCircumference(ProjectedPoint points[QUAD_VERTEX_COUNT])
{
    return length(points[0].pc - points[1].pc) +
        length(points[1].pc - points[2].pc) +
        length(points[2].pc - points[3].pc) +
        length(points[3].pc - points[0].pc);
}

// only sort a quad's extremes (highest and lowest)
void SortQuadExtremesVertically(inout ProjectedPoint points[QUAD_VERTEX_COUNT])
{
    int y0 = points[0].pc.y;
    int y1 = points[1].pc.y;
    int y2 = points[2].pc.y;
    int y3 = points[3].pc.y;

    // Find the highest and lowest y components
    int lowestY = min(min(y0, y1), min(y2, y3));
    int highestY = max(max(y0, y1), max(y2, y3));

    // Find the lowest vector
    ProjectedPoint lowest;
    if (y0 == lowestY)
    {
        lowest = points[0];
    }
    else if (y1 == lowestY)
    {
        lowest = points[1];
    }
    else if (y2 == lowestY)
    {
        lowest = points[2];
    }
    else
    {
        lowest = points[3];
    }

    // Find the highest vector
    ProjectedPoint highest;
    if (y0 == highestY)
    {
        highest = points[0];
    }
    else if (y1 == highestY)
    {
        highest = points[1];
    }
    else if (y2 == highestY)
    {
        highest = points[2];
    }
    else
    {
        highest = points[3];
    }

    // Find the first middle vector
    ProjectedPoint middle1;
    if (y0 != highest.pc.y && y0 != lowest.pc.y)
    {
        middle1 = points[0];
    }
    else if (y1 != highest.pc.y && y1 != lowest.pc.y)
    {
        middle1 = points[1];
    }
    else if (y2 != highest.pc.y && y2 != lowest.pc.y)
    {
        middle1 = points[2];
    }
    else
    {
        middle1 = points[3];
    }

    // Find the second middle vector
    ProjectedPoint middle2;
    if (y0 != highest.pc.y && y0 != lowest.pc.y && y0 != middle1.pc.y)
    {
        middle2 = points[0];
    }
    else if (y1 != highest.pc.y && y1 != lowest.pc.y && y1 != middle1.pc.y)
    {
        middle2 = points[1];
    }
    else if (y2 != highest.pc.y && y2 != lowest.pc.y && y2 != middle1.pc.y)
    {
        middle2 = points[2];
    }
    else
    {
        middle2 = points[3];
    }

    // Assign the sorted vectors back to the input array
    points[0] = lowest;
    points[1] = middle1;
    points[2] = middle2;
    points[3] = highest;
}