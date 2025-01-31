float4 WorldSpaceToCubemapUV(float3 localPosition, int cubemapIndex)
{
    // determine the face index based on the dominant axis
    int faceIndex;
    float3 absPos = abs(localPosition);
    float3 faceNormal;

    if (absPos.x >= absPos.y && absPos.x >= absPos.z)
    {
        faceIndex = (localPosition.x > 0) ? 0 : 1; // +x or -x
        faceNormal = float3(sign(localPosition.x), 0, 0);
    }
    else if (absPos.y >= absPos.x && absPos.y >= absPos.z)
    {
        faceIndex = (localPosition.y > 0) ? 2 : 3; // +y or -y
        faceNormal = float3(0, sign(localPosition.y), 0);
    }
    else
    {
        faceIndex = (localPosition.z > 0) ? 4 : 5; // +z or -z
        faceNormal = float3(0, 0, sign(localPosition.z));
    }

    // project local position onto the selected cubemap face
    float3 projected = localPosition - faceNormal * dot(localPosition, faceNormal);

    // calculate UV coordinates relative to the selected face
    float2 uv;
    if (faceIndex < 2)      // +x or -x
        uv = float2(-projected.z, projected.y) / abs(localPosition.x);
    else if (faceIndex < 4) // +y or -y
        uv = float2(projected.x, -projected.z) / abs(localPosition.y);
    else                    // +z or -z
        uv = float2(projected.x, projected.y) / abs(localPosition.z);

    // transform UV to [0, 1] range
    uv = uv * 0.5 + 0.5;

    if (faceIndex == 1)
        uv.x = 1.0 - uv.x; // -x: Flip horizontally
    if (faceIndex == 3)
        uv.y = 1.0 - uv.y; // -y: Flip vertically
    if (faceIndex == 5)
        uv.x = 1.0 - uv.x; // -z: flip horizontally

    float depth = length(localPosition);
    return float4(uv, depth, faceIndex);
}

float4 SampleBilinear(RWTexture2DArray<float4> tex, float2 uv, uint2 textureSize, int slice)
{
    // Convert normalized UV to texel space
    float2 texelPos = uv * textureSize;
    
    // Get integer and fractional parts
    uint2 texelBase = (uint2) texelPos; // Top-left texel
    float2 f = frac(texelPos); // Fractional part for interpolation
    uint2 off = uint2(1, 0);
    
    // Sample four neighboring texels
    float4 c00 = tex[uint3(texelBase + off.yy, slice)];
    float4 c10 = tex[uint3(texelBase + off.xy, slice)];
    float4 c01 = tex[uint3(texelBase + off.yx, slice)];
    float4 c11 = tex[uint3(texelBase + off.xx, slice)];

    // Bilinear interpolation
    float4 c0 = lerp(c00, c10, f.x);
    float4 c1 = lerp(c01, c11, f.x);
    return lerp(c0, c1, f.y);
}