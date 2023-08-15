struct ProjectedPoint
{
    // projected uv coordinates
    float2 uv;

    // absolute projected coordinates
    int2 pc;

    // corresponding original coordinates
    int2 oc;

    // projected depth value (non-normalized)
    float d;
    
    // color at the original point
    float4 col;
    
    // whether that point is onscreen
    bool valid;
};

struct Triangle
{
    // vertex points ascending by height
    ProjectedPoint v0, v1, v2;
};