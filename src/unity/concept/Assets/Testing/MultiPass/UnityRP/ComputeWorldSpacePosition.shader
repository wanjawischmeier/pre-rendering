Shader "Hidden/ComputeWorldSpacePosition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            #define ARRAY_SIZE 3
            #define N_VALUES_TO_SORT 3
            #define FCLIP 30

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 raySphereIntersectionPoint(
                float3 rayPos0, float3 rayPos1,
                float3 spherePos, float sphereRadius
            ) {
                float3 d = normalize(rayPos1 - rayPos0);
                float3 m = rayPos0 - spherePos;
                float b = dot(m, d);
                float c = dot(m, m) - sphereRadius * sphereRadius;
                
                // exit if r’s origin outside s (c > 0) and r pointing away from s (b > 0) 
                if (b > 0.0f && c > 0.0f) return float3(0, 0, 0);
                float discriminant = b * b - c;
    
                // a negative discriminant corresponds to ray missing sphere
                if (discriminant < 0.0f) return float3(0, 0, 0);
    
                // ray now found to intersect sphere, compute largest t value of intersection
                float t = -b + sqrt(discriminant);
    
                // compute point at t on ray (clamp to 0?)
                return rayPos0 + t * d;
            }

            float2 vectorToLonLat(float3 vec, float dist)
            {
                return float2(
                    atan2(vec.x, vec.z),
                    -acos(vec.y / dist)
                );
            }
            
            float4 getViewRayCoordinates(float3 a, float3 b, float3 c, int index)
            {
                // float numerator = length(cross(p - o, p - d));
                // return numerator / length(d - o);
    
                float3 d = normalize(b - a);
                float t = dot(c - a, d) / dot(d, d);
                if (t < 0 && false)
                {
                    return -1;
                }
    
                float3 i = a + t * d;
                float3 ci = i - c;
                float distance = length(ci);
                float3 p = raySphereIntersectionPoint(a, b, c, FCLIP);
                float2 ll = vectorToLonLat(p, FCLIP);
                return float4(ll, distance, index);
            }

            float4 test(float3 a, float3 b, float3 c, int index)
            {
                float3 d = normalize(b - a);
                float t = dot(c - a, d) / dot(d, d);
                return float4(0, 0, 0, 0);
            }

            uniform sampler2D _CameraDepthTexture, _Input0, _Input1, _Input2;
            uniform float PI;
            uniform float3 PCAM, P0, P1, P2;
            uniform float3 POINTS[ARRAY_SIZE];
            uniform float4x4 _ViewProjInv;
            float4 tc[ARRAY_SIZE];

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                if (depth == 0)
                {
                    depth = 1;
                }
    
                float4 clipPos = float4(i.uv * 2.0 - 1.0, depth, 1.0);
                float4 worldPos = mul(_ViewProjInv, clipPos);
                float3 P = worldPos.xyz / worldPos.w;
                if (depth != 1)
                {
                    return fixed4(P, 1);
                }
                
                [unroll]
                for (int i = 0; i < ARRAY_SIZE; ++i)
                {
                    // tc[i] = getViewRayCoordinates(PCAM, P, P, i);
                    tc[i] = getViewRayCoordinates(PCAM, P, POINTS[i], i);
                }
                /*
                // Insertion sort for the smallest N values based on z component
                [unroll]
                for (i = 1; i < N_VALUES_TO_SORT; ++i)
                {
                    float4 key = tc[i];
                    int j = i - 1;
                    
                    [unroll]
                    while (j >= 0 && tc[j].z > key.z)
                    {
                        tc[j + 1] = tc[j];
                        --j;
                    }
                    
                    tc[j + 1] = key;
                }
                */
                float4 second, lowest = tc[0];
                [unroll]
                for (i = 1; i < ARRAY_SIZE; ++i)
                {
                    if (tc[i].z < lowest.z)
                    {
                        second = lowest;
                        lowest = tc[i];
                    }
                }
                // lowest = second;
    
    
                fixed4 col;
                if (tc[0].z != -1 && tc[0].z < tc[1].z && tc[0].z < tc[2].z)
                {
                    col = fixed4(tc[0].z, 0, 0, 1);
                }
                else if (tc[1].z != -1 && tc[1].z < tc[0].z && tc[1].z < tc[2].z)
                {
                    col = fixed4(0, tc[1].z, 0, 1);
                }
                else if (tc[2].z != -1)
                {
                    col = fixed4(0, 0, tc[2].z, 1);
                }
                else
                {
                    col = fixed4(1, 0, 1, 1);
                }
    
                float2 tc = float2(lowest.x / (PI * 2) + 0.5, lowest.y / PI);
                switch (lowest.w)
                {
                    case 0:
                        col = tex2D(_Input0, tc);
                        break;
                    case 1:
                        col = tex2D(_Input1, tc);
                        break;
                    case 2:
                        col = tex2D(_Input2, tc);
                        break;
                    default:
                        col = fixed4(1, 0, 1, 1);
                        break;

                }
                return col;
            }
            ENDCG
        }
    }
}
