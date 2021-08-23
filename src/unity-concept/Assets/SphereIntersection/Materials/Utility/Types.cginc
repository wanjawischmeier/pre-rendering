struct latlon
{
    float lat;
    float lon;
};


latlon vectorToLatLon(float3 vector3, float sphere_radius = 1)
{
    latlon result;
    result.lat = acos(vector3.y / sphere_radius);
    result.lon = atan2(vector3.x, vector3.z);
    
    return result;
}
float3 latLonToVector(latlon latlon, float sphere_radius = 1)
{
    return float3(
        sphere_radius * sin(latlon.lon) * sin(latlon.lat),
        sphere_radius * cos(latlon.lat),
        sphere_radius * cos(latlon.lon) * sin(latlon.lat)
    );
}

latlon switchLatlon(latlon inp)
{
    latlon result;
    result.lat = inp.lon;
    result.lon = inp.lat;
    
    return result;
}

latlon normalizedToLatLon(latlon normalized, float PI)
{
    latlon result;
    result.lat = normalized.lat * (PI * 1);
    result.lon = normalized.lon * (PI * 2);
    
    return result;
}
latlon latLonToNormalized(latlon inp, float PI)
{
    latlon result;
    result.lat = inp.lat / (PI * 1);
    result.lon = inp.lon / (PI * 2);
    
    return result;
}

latlon float2ToLatLon(float2 inp, float PI)
{
    latlon result;
    result.lat = inp.y * (PI * 1);
    result.lon = inp.x * (PI * 2);
    
    return result;
}
float2 latLonToFloat2(latlon inp, float PI)
{
    return float2(
        inp.lon / (PI * 2),
        inp.lat / (PI * 1)
    );
}

latlon toLatLon(float2 inp)
{
    latlon result;
    result.lat = inp.x;
    result.lon = inp.y;
    
    return result;
}
float2 toFloat2(latlon inp) { return float2(inp.lat, inp.lon); }
float4 toFloat4(float inp)  { return float4(inp, inp, inp, 1); }
float4 toFloat4(float2 inp) { return float4(inp.xy, 0, 1); }
float4 toFloat4(float3 inp) { return float4(inp.xyz, 1); }
float4 toFloat4(latlon inp) { return float4(inp.lat, inp.lon, 0, 1); }

float map(float value, float min1, float max1, float min2, float max2)
{
    return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
}