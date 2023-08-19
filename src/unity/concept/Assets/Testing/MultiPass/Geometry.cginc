struct ProjectedPoint
{
    // projected uv coordinates
    float2 uv;

    // absolute projected coordinates
    int2 pc;

    // corresponding original uv coordinates
    float2 oc;

    // projected depth value (non-normalized)
    float d;
};

struct Triangle
{
    // vertex points ascending by height
    ProjectedPoint v0, v1, v2;
};