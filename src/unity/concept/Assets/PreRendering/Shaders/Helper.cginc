float PI, PI2, NegativeInfinity, PositiveInfinity;

struct circularSamples
{
    float4 s0, s1, s2, s3, s4, s5, s6, s7, s8;
};

struct cRay
{
    float3 pos, dir;
};

struct boundingBox
{
    float3 min, max;
};

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

bool rayAABBIntersection(cRay ray, boundingBox aabb, out float tmin, out float tmax)
{
    float3 invD = rcp(ray.dir);
    float3 t0s = (aabb.min - ray.pos) * invD;
    float3 t1s = (aabb.max - ray.pos) * invD;

    float3 tsmaller = min(t0s, t1s);
    float3 tbigger = max(t0s, t1s);
    
    tmin = max(NegativeInfinity, max(tsmaller[0], max(tsmaller[1], tsmaller[2])));
    tmax = min(PositiveInfinity, min(tbigger[0], min(tbigger[1], tbigger[2])));

    return (tmin < tmax);
}

// Based on https://stackoverflow.com/a/26357357/13215204
float2 calculateNormals(circularSamples s)
{
    float2 n = float2(
	    -(s.s2.a - s.s0.a + 2 * (s.s5.a - s.s3.a) + s.s8.a - s.s6.a),
	    -(s.s6.a - s.s0.a + 2 * (s.s7.a - s.s1.a) + s.s8.a - s.s2.a)
	);

    return normalize(n) * 0.5 + 0.5;
}

float4 blur(circularSamples s, float amount)
{
    float4 averaged = (s.s0 + s.s1 + s.s2 + s.s3 + s.s4 + s.s5 + s.s6 + s.s7 + s.s8) / 9;
    return averaged * amount + s.s4 * (1 - amount);
}