void unpack16b(uint v, out uint v0, out uint v1)
{
    v0 = v & 0xFFFF;
    v1 = v >> 16;
}

void unpack8b(uint v, out uint v0, out uint v1)
{
    v0 = v & 0xFFFF;
    v1 = v >> 16;
}

half4 normalizeColor16b(uint r, uint g, uint b, uint a)
{
    return half4(r, g, b, a) / (float)0xFFFF;
}

half4 rawTex2D(StructuredBuffer<uint> rawTexture, float2 uv, uint2 resolution, uint offset)
{
    int2 tc = uv * resolution;
    int idx = (tc.x + (resolution.y - tc.y - 1) * resolution.x + offset) * 2;

    uint bgPacked = rawTexture[idx];
    uint raPacked = rawTexture[idx + 1];

    uint r, g, b, a;

    unpack16b(bgPacked, b, g);
    unpack16b(raPacked, r, a);

    return normalizeColor16b(r, g, b, a);
}

half rawTex2DA(StructuredBuffer<uint> rawTexture, float2 uv, uint2 resolution, uint offset)
{
    int2 tc = uv * resolution;
    int idx = (tc.x + (resolution.y - tc.y - 1) * resolution.x + offset) * 2;

    uint raPacked = rawTexture[idx + 1];
    uint a, _;
    unpack16b(raPacked, _, a);

    return a / (float)0xFFFF;
}