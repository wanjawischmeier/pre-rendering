#include "Types.cginc"

latlon gnomonicProjection(float2 position, float fieldOfView, latlon rotation, float PI)
{
    
    float PI2 = 2 * PI;
    
    float x = PI2 * (position.x - 0.5);
    float y = PI * (position.y - 0.5);

    float p = sqrt(x * x + y * y);
    float c = atan2(p, fieldOfView);

    float sinC = sin(c);
    float cosC = cos(c);
    float sinPhi1 = sin(rotation.lat);
    float cosPhi1 = cos(rotation.lat);

    float phi = asin(cosC * sinPhi1 + y * sinC * cosPhi1 / p);
    float lambda = rotation.lon + atan2(x * sinC, (p * cosPhi1 * cosC - y * sinPhi1 * sinC));
    
    latlon result;
    result.lat = phi;
    result.lon = lambda;
    
    return result;
}

float2 raySphereIntersection(
    float3 rayPos, float3 rayDir,
    float3 spherePos, float sphereRadius = 1
)
{
    float3 o_minus_c = rayPos - spherePos;

    float p = dot(rayDir, o_minus_c);
    float q = dot(o_minus_c, o_minus_c) - (sphereRadius * sphereRadius);
    
    float discriminant = (p * p) - q;

    float dRoot = sqrt(discriminant);
    return float2(
        -p - dRoot,
        -p + dRoot
    );
}

latlon translateInsideSphere(latlon inp, float3 P1, float depth, float PI, float3 C = float3(0, 0, 0))
{
    float r1 = 1;
    float r2 = 1;
    r2 = depth;
    // P1 *= 1 - depth;
    
    inp = normalizedToLatLon(inp, PI);
    
    // 1. latlon to point on sphere
    float3 P2 = latLonToVector(inp, r1);
    
    // 2. intersection distances of P1 and P2
    float2 t = raySphereIntersection(P1, P2, C, r2);
    float t1 = t.x; float t2 = t.y;
    
    // 3. get intersection points
    float3 P3 = P1 + float3(P2.x * t1, P2.y * t1, P2.z * t1);
    float3 P4 = P1 + float3(P2.x * t2, P2.y * t2, P2.z * t2);
    
    // 4. get and normalize result
    latlon result;
    result = vectorToLatLon(P4, r2);
    result = latLonToNormalized(result, PI);
    result = switchLatlon(result);
    
    return result;
}