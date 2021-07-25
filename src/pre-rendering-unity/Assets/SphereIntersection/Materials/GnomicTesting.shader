Shader "Hidden/GnomicTesting"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Phi1 ("Phi1", Float) = 0
        _Lambda0 ("Lambda0", Float) = 0
        _Fov ("Field Of View", Float) = 0
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float2 gnomicProjection(float2 pos, float fov, float phi1, float lambda0, float PI)
            {
                float PI2 = 2 * PI;

                float x = PI2 * (pos.x - 0.5);
                float y = PI * (pos.y - 0.5);

                float p = sqrt(x * x + y * y);
                float c = atan2(p, fov);

                float sinC = sin(c); float cosC = cos(c);
                float sinPhi1 = sin(phi1); float cosPhi1 = cos(phi1);

                float phi = asin(cosC * sinPhi1 + y * sinC * cosPhi1 / p);
                float lambda = lambda0 + atan2(x * sinC, (p * cosPhi1 * cosC - y * sinPhi1 * sinC));

                return float2(lambda / (PI * 2.0) + 0.5, (phi / PI) + 0.5);
            }

            float2 raySphereIntersection(
                float3 rayPos, float3 rayDir,
                float3 spherePos, float sphereRadius = 1
            ) {
                float3 o_minus_c = rayPos - spherePos;

                float p = dot(rayDir, o_minus_c);
                float q = dot(o_minus_c, o_minus_c) - (sphereRadius * sphereRadius);

                float discriminant = (p * p) - q;
                if (discriminant < 0.0f) return float2(0, 0);

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

            float4 toOut(float inp) {
                return float4(inp, inp, inp, 1);
            }

            float4 toOut(float2 inp) {
                return float4(inp.x, inp.y, 0, 1);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _Phi1;
            float _Lambda0;
            float _Fov;
            float4 _P1W;

            fixed4 frag(v2f i) : SV_Target
            {
                float PI = 3.14159;

                float2 tc = gnomicProjection(i.uv, _Fov, _Phi1, _Lambda0, PI);

                fixed4 col = tex2D(_MainTex, tc);

                // return toOut(tc);
                return col;
            }
            ENDCG
        }
    }
}
