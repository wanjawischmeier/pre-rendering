float PI, PI2;

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

float2 gnomonicProjection(float2 pos, float fov, float phi1, float lambda0)
{
    float x = PI2 * (pos.x - 0.5);
    float y = PI * (pos.y - 0.5);

    float p = sqrt(x * x + y * y);
    float c = atan2(p, fov);

    float sinC = sin(c); float cosC = cos(c);
    float sinPhi1 = sin(phi1); float cosPhi1 = cos(phi1);

    float phi = asin(cosC * sinPhi1 + y * sinC * cosPhi1 / p);
    float lambda = lambda0 + atan2(x * sinC, (p * cosPhi1 * cosC - y * sinPhi1 * sinC));

    return float2(lambda / PI2 + 0.5, phi / PI + 0.5);
}