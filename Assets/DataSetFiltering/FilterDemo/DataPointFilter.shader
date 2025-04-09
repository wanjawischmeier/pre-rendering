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
                    return float4(1, 0.5, 0, 1);  // Grayscale output
                }

                float blendedValue = BlendSimilarDataPoints(pos, closestValue);

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
