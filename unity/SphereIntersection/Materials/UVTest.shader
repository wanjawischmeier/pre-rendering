Shader "Unlit/UVTest"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Phi1("Phi1", Float) = 0
        _Lambda0("Lambda0", Float) = 0
        _Fov("Field Of View", Float) = 0
        _R1("Radius 1", Range(0, 2)) = 0
        _R2("Radius 2", Range(0, 2)) = 0
        _X("X", Range(-1, 1)) = 0
        _Y("Y", Range(-1, 1)) = 0
        _Z("Z", Range(-1, 1)) = 0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 100

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                // make fog work
                #pragma multi_compile_fog

                #include "UnityCG.cginc"

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    float4 vertex : SV_POSITION;
                };

                float2 raySphereIntersection(
                    float3 rayPos, float3 rayDir,
                    float3 spherePos, float sphereRadius = 1
                ) {
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

                float2 vectorToLatLon(float3 vector3, float sphere_radius = 1) {
                    return float2(
                        acos(vector3.y / sphere_radius),
                        atan2(vector3.x, vector3.z)
                    );
                }

                float3 latLonToVector(float2 latlon, float sphere_radius = 1) {
                    return float3(
                        sphere_radius * sin(latlon.y) * sin(latlon.x),
                        sphere_radius * cos(latlon.x),
                        sphere_radius * cos(latlon.y) * sin(latlon.x)
                    );
                }

                float2 normalizedToLatLon(float2 normalized, float PI) {
                    return float2(
                        normalized.x * PI,
                        normalized.y * (PI * 2)
                    );
                }

                float2 latLonToNormalized(float2 latlon, float PI) {
                    return float2(
                        latlon.x / (PI * 1),
                        latlon.y / (PI * 2)
                    );
                }

                float4 toOut(float inp) {
                    return float4(inp, inp, inp, 1);
                }

                float4 toOut(float2 inp) {
                    return float4(inp.x, inp.y, 0, 1);
                }

                float4 toOut(float3 inp) {
                    return float4(inp.x, inp.y, inp.z, 1);
                }

                sampler2D _MainTex;
                float _Phi1;
                float _Lambda0;
                float _Fov;
                float _R1;
                float _R2;
                float _X;
                float _Y;
                float _Z;

                v2f vert(appdata v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    float PI = 3.14159;
                    float3 P1 = float3(_X, _Y, _Z);

                    float r1 = _R1; float r2 = _R2;

                    // 0. gnomonic projection and get depth
                    float2 tc = i.uv;
                    float d = tex2D(_MainTex, tc).a;
                    r2 += d;
                    // return toOut(d);

                    // Center sphere
                    float3 C = float3(0, 0, 0);

                    // Tc to latlon and to radians
                    float2 latlon = normalizedToLatLon(float2(tc.y, tc.x), PI);

                    // 1. To point on sphere
                    float3 P2 = latLonToVector(latlon, r1);

                    // 2. Ray from P1 to P2 (skipped)
                    // 3. Get intersection P3 and P4
                    float2 t = raySphereIntersection(P1, P2, C, r2);
                    float t1 = t.x; float t2 = t.y;

                    float3 P3 = P1 + float3(P2.x * t1, P2.y * t1, P2.z * t1);
                    float3 P4 = P1 + float3(P2.x * t2, P2.y * t2, P2.z * t2);

                    // 4. Get outlatlon
                    float2 outlatlon = vectorToLatLon(P4, r2);
                    // float3 outvec = latLonToVector(outlatlon, r);
                    outlatlon = latLonToNormalized(outlatlon, PI);
                    outlatlon = float2(outlatlon.y, outlatlon.x);

                    // return toOut(tc.x);
                    // return toOut(outlatlon.x + 0.5);
                    // return toOut(P4.xyz);
                    // return toOut(outlatlon.x);
                    fixed4 col = tex2D(_MainTex, outlatlon);


                    float4 conv = float4(outlatlon - tc, 0, 1);

                    return col;
                }
                ENDCG
            }
        }
}
