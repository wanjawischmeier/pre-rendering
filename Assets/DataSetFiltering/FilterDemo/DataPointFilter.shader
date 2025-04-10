Shader "Unlit/DataPointFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Scale", Float) = 1
        _Threshold ("Threshold", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct DataPoint {
                float2 pos;
                float value;
            };

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

            sampler2D _MainTex;
            float _Scale, _Threshold;
            float4 _MainTex_ST;
            float4 _DataPoints[10];

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            void FindThreeClosest(float2 pos, out DataPoint p0, out DataPoint p1, out DataPoint p2)
            {
                float d0 = 99999, d1 = 99999, d2 = 99999;
                p0 = p1 = p2 = (DataPoint)0;
                DataPoint tmp;

                for (int i = 0; i < 10; i++)
                {
                    float4 dp = _DataPoints[i];
                    if (dp.a != 1) continue;

                    float dist = distance(pos, dp.xy);
                    tmp.pos = dp.xy;
                    tmp.value = dp.z;

                    if (dist < d0)
                    {
                        d2 = d1; p2 = p1;
                        d1 = d0; p1 = p0;
                        d0 = dist; p0 = tmp;
                    }
                    else if (dist < d1)
                    {
                        d2 = d1; p2 = p1;
                        d1 = dist; p1 = tmp;
                    }
                    else if (dist < d2)
                    {
                        d2 = dist; p2 = tmp;
                    }
                }
            }

            float3 BarycentricWeights(float2 p, float2 a, float2 b, float2 c)
            {
                float2 v0 = b - a;
                float2 v1 = c - a;
                float2 v2 = p - a;

                float d00 = dot(v0, v0);
                float d01 = dot(v0, v1);
                float d11 = dot(v1, v1);
                float d20 = dot(v2, v0);
                float d21 = dot(v2, v1);

                float denom = d00 * d11 - d01 * d01;
                if (abs(denom) < 1e-5) return float3(1, 0, 0); // degenerate triangle fallback

                float v = (d11 * d20 - d01 * d21) / denom;
                float w = (d00 * d21 - d01 * d20) / denom;
                float u = 1.0 - v - w;

                return float3(u, v, w);
            }



            // Find the closest valid data point
            float GetClosestDataValue(float2 pos, out float closestDist)
            {
                float minDist = 99999.0;
                float closestValue = 0.0;

                for (int i = 0; i < 10; i++)
                {
                    float4 dp = _DataPoints[i];
                    if (dp.a != 1) continue;

                    float dist = distance(pos, dp.xy);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestValue = dp.z;
                    }
                }

                closestDist = minDist;
                return closestValue;
            }

            // Blend values of nearby data points similar in value to the reference
            float BlendSimilarDataPoints(float2 pos, float refValue)
            {
                float totalWeight = 0.0;
                float weightedValueSum = 0.0;
                float epsilon = 0.001;

                for (int i = 0; i < 10; i++)
                {
                    float4 dp = _DataPoints[i];
                    if (dp.a != 1) continue;

                    float valueDiff = abs(dp.z - refValue);
                    if (valueDiff > _Threshold) continue;

                    float dist = distance(pos, dp.xy);
                    float weight = 1.0 / (dist + epsilon);

                    weightedValueSum += weight * dp.z;
                    totalWeight += weight;
                }

                return (totalWeight > 0.0) ? (weightedValueSum / totalWeight) : refValue;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 pos = (i.uv - 0.5) * 2 * _Scale;
                
                float closestDist;
                float closestValue = GetClosestDataValue(pos, closestDist);
                if (closestDist < 0.1)
                {
                    return float4(0, 0.5, closestValue, 1);  // Grayscale output
                }

                float blendedValue = BlendSimilarDataPoints(pos, closestValue);
                
                
                DataPoint p0, p1, p2;
                FindThreeClosest(pos, p0, p1, p2);

                float3 bary = BarycentricWeights(pos, p0.pos, p1.pos, p2.pos);
                blendedValue = bary.x * p0.value + bary.y * p1.value + bary.z * p2.value;
                
                return float4(blendedValue.xxx, 1);  // Grayscale output
            }
            /*
            fixed4 frag (v2f i) : SV_Target
            {
                float2 pos = (i.uv - 0.5) * 2 * _Scale;

                float closestDist = 9999;
                float closestValue = 0;
                for (int i = 0; i < 10; i++)
                {
                    float4 dataPoint = _DataPoints[i];
                    if (dataPoint.a != 1) continue;

                    float dist = distance(pos, dataPoint.xy);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestValue = dataPoint.z;
                    }
                }

                return float4(closestValue.xxx, 1);
                return float4(closestDist.xxx, 1);
                return float4(pos, 0, 1);
            }
            */
            ENDCG
        }
    }
}
