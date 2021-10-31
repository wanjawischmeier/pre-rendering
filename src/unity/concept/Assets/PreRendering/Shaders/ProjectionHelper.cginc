const float PI  = 3.141592653589793;
const float PI2 = 6.283185307179586;

float magnitude(float3 vec)
{
    return sqrt(
        vec.x * vec.x +
        vec.y * vec.y +
        vec.z * vec.z
    );
}

float2 normalizedToLatLon(float2 normalized)
{
    return float2(
        normalized.x * PI,
        normalized.y * PI2 + PI
    );
}

float2 latLonToNormalized(float2 latLon)
{
    return float2(
        latLon.y / PI2 + 0.5,
        latLon.x / PI
    );
}

float3 latLonToVector(float2 latLon, float dist = 1)
{
    return float3(
        dist * sin(latLon.y) * sin(latLon.x),
        dist * cos(latLon.x),
        dist * cos(latLon.y) * sin(latLon.x)
    );
}

float2 vectorToLatLon(float3 vec, float dist = 1)
{
    return float2(
        acos(vec.y / dist),
        atan2(vec.x, vec.z)
    );
}

float2 translateLatLon(float2 latLon, float3 translation, float dist = 1)
{
    float3 P = latLonToVector(latLon, dist);
    P += translation;
    
    float d = magnitude(P);
    return vectorToLatLon(P, d);
}