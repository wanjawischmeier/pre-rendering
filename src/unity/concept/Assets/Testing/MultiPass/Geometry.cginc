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