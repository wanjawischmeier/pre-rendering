Shader "Unlit/DataPointFilter"
{
    Properties
    {
        _Scale ("Scale", Float) = 1
        _Threshold ("Threshold", Float) = 1
        _Epsilon ("Epsilon", Float) = 1
        _DistWeight ("Distance Weight", Float) = 0
        _TileInfluenceRadius ("Tile Influcence Radius", Float) = 2
        _EdgeFalloffFactor ("Edge Falloff Factor", Float) = 1
        _DistWeight2 ("Distance Weight 2", Float) = 0
        _EdgeFalloffExp ("Edge Falloff Exponent", Float) = 1
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

            #include "UnityCG.cginc"

            struct DataPoint {
                float2 pos;
                float value;
            };
            
            struct Candidate {
                float dist;
                DataPoint pt;
            };

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

            float _Scale, _Threshold, _Epsilon, _DistWeight;
            float _TileInfluenceRadius, _EdgeFalloffFactor, _Epsilon2, _DistWeight2, _EdgeFalloffExp;
            float4 _MainTex_ST;
            float4 _DataPoints[10];
            #define MAX_POINTS 10
            #define MAX_CANDIDATES 4

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            bool PointInTriangle(float2 p, float2 a, float2 b, float2 c)
            {
                float2 v0 = c - a;
                float2 v1 = b - a;
                float2 v2 = p - a;

                float d00 = dot(v0, v0);
                float d01 = dot(v0, v1);
                float d11 = dot(v1, v1);
                float d20 = dot(v2, v0);
                float d21 = dot(v2, v1);

                float denom = d00 * d11 - d01 * d01;
                if (abs(denom) < 1e-5) return false;

                float v = (d11 * d20 - d01 * d21) / denom;
                float w = (d00 * d21 - d01 * d20) / denom;
                float u = 1.0 - v - w;

                return (u >= 0.0 && v >= 0.0 && w >= 0.0 && u <= 1.0 && v <= 1.0 && w <= 1.0);
            }

            bool FindContainingTriangle(float2 pos, out DataPoint a, out DataPoint b, out DataPoint c)
            {
                Candidate candidates[MAX_CANDIDATES];
                int count = 0;

                // Gather and sort top N closest points
                for (int i = 0; i < MAX_POINTS; i++)
                {
                    float4 dp = _DataPoints[i];
                    if (dp.a != 1) continue;

                    float dist = distance(pos, dp.xy);

                    // insert sorted
                    int insertAt = count;
                    for (int j = 0; j < count; j++)
                    {
                        if (dist < candidates[j].dist)
                        {
                            insertAt = j;
                            break;
                        }
                    }

                    if (count < MAX_CANDIDATES)
                    {
                        // Shift to make room
                        for (int j = min(count, MAX_CANDIDATES - 1); j > insertAt; j--)
                            candidates[j] = candidates[j - 1];

                        candidates[insertAt].dist = dist;
                        candidates[insertAt].pt.pos = dp.xy;
                        candidates[insertAt].pt.value = dp.z;

                        count++;
                    }
                }

                // Try all triangle combinations (brute force inside top N)
                for (int i = 0; i < count; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        for (int k = j + 1; k < count; k++)
                        {
                            DataPoint p0 = candidates[i].pt;
                            DataPoint p1 = candidates[j].pt;
                            DataPoint p2 = candidates[k].pt;

                            if (PointInTriangle(pos, p0.pos, p1.pos, p2.pos))
                            {
                                a = p0; b = p1; c = p2;
                                return true;
                            }
                        }
                    }
                }

                // Fallback to 3 closest if none found
                a = candidates[0].pt;
                b = candidates[1].pt;
                c = candidates[2].pt;
                return false;
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

            bool IsNearEdge(float3 bary, float threshold)
            {
                return bary.x < threshold || bary.y < threshold || bary.z < threshold;
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

            float PowerFalloff(float dist, float R)
            {
                float t = saturate(dist / R); // clamp to [0, 1]
                return 1.0 - pow(t, _EdgeFalloffExp);
            }

            // Blend values of nearby data points similar in value to the reference
            float ComputeWeightedBlend(float2 pos)
            {
                float totalWeight = 0.0;
                float weightedValueSum = 0.0;
                float closestDist = 99999.0;
                float secondClosestDist = 99999.0;
                float closestValue = 0.0;
                float secondClosestValue = 0.0;

                for (int i = 0; i < 10; i++)
                {
                    float4 dp = _DataPoints[i];
                    if (dp.a != 1) continue;

                    float dist = distance(pos, dp.xy);
                    if (dist < closestDist)
                    {
                        // New closest -> demote old closest to second
                        secondClosestDist = closestDist;
                        secondClosestValue = closestValue;

                        closestDist = dist;
                        closestValue = dp.z;
                    }
                    else if (dist < secondClosestDist)
                    {
                        // Not closer than the closest, but closer than second
                        secondClosestDist = dist;
                        secondClosestValue = dp.z;
                    }

                    float weight = 1.0 / pow(dist, _Epsilon + (dist * _DistWeight));

                    weightedValueSum += weight * dp.z;
                    totalWeight += weight;
                }
                
                if (abs(closestValue - secondClosestValue) > _Threshold)
                {
                    return closestValue;
                }

                return weightedValueSum / totalWeight;
            }


            float ComputeWeightedBlend2(float2 pos, float refValue)
            {
                float totalWeight = 0.0;
                float weightedValueSum = 0.0;
                float closestValue = 0.0;
                float secondClosestValue = 0.0;
                float closestDist = 99999.0;

                for (int i = 0; i < 10; i++)
                {
                    float4 dp = _DataPoints[i];
                    if (dp.a != 1) continue;

                    float dist = distance(pos, dp.xy);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        secondClosestValue = closestValue;
                        closestValue = dp.z;
                    }
                    
                    // float dWeight = PowerFalloff(dist, _TileInfluenceRadius);
                    float weight = 1.0 / pow(dist, _Epsilon + (dist * _DistWeight));
                    // weight *= dWeight;

                    weightedValueSum += weight * dp.z;
                    totalWeight += weight;
                }

                if (totalWeight == 0 || abs(closestValue - secondClosestValue) > _Threshold)
                {
                    return 0.5;
                    return closestValue * PowerFalloff(closestDist, _TileInfluenceRadius);
                }

                // return totalWeight;
                return (weightedValueSum / totalWeight) * PowerFalloff(closestDist, _TileInfluenceRadius);;
            }

            float ComputeWeightedBlend3(float2 pixelWorldPos, float closestValue)
            {
                float totalWeight = 0.0;
                float weightedSum = 0.0;
                // float closestValue = 0.0;
                float secondClosestValue = 0.0;
                float closestDist = 99999.0;

                [unroll]
                for (int i = 0; i < 10; i++)
                {
                    float4 dp = _DataPoints[i];
                    if (dp.a != 1)
                        continue;

                    float dist = distance(dp.xy, pixelWorldPos);

                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        secondClosestValue = closestValue;
                        closestValue = dp.z;
                    }

                    float w = PowerFalloff(dist, _TileInfluenceRadius); // or dp.w if per-point

                    if (w > 1e-4)
                    {
                        totalWeight += w;
                        weightedSum += w * dp.z;
                    }
                }

                if (totalWeight < 1e-4 || abs(closestValue - secondClosestValue) > _Threshold)
                {
                    return closestValue * PowerFalloff(closestDist, _TileInfluenceRadius);
                }

                return weightedSum / totalWeight;
            }


            fixed4 frag (v2f i) : SV_Target
            {
                float2 pos = (i.uv - 0.5) * 2 * _Scale;
                
                // Add borders around 1x1 tiles of the scaled uv
                float2 tilePos = frac(pos);
                float borderWidth = 0.01;
                if (tilePos.x < borderWidth || tilePos.x > 1.0 - borderWidth || tilePos.y < borderWidth || tilePos.y > 1.0 - borderWidth)
                {
                    return float4(1, 0, 0, 1); // white border
                }

                float closestDist;
                float closestValue = GetClosestDataValue(pos, closestDist);
                if (closestDist < 0.05)
                {
                    return float4(0, 0.5, closestValue, 1);  // Grayscale output
                }

                float blendedValue = ComputeWeightedBlend(pos);
                // float blendedValue = ComputeWeightedBlend2(pos, closestValue);
                
                /*
                DataPoint p0, p1, p2;
                // FindThreeClosest(pos, p0, p1, p2);
                bool triangleFound = FindContainingTriangle(pos, p0, p1, p2);
                if (!triangleFound)
                {
                    return float4(0.5, 0, 0, 1);  // Grayscale output
                }

                float3 bary = BarycentricWeights(pos, p0.pos, p1.pos, p2.pos);
                
                // Debug: highlight triangle edges
                float edgeThreshold = 0.0001;
                if (IsNearEdge(bary, edgeThreshold))
                {
                    return float4(1, 0, 0, 1); // red for triangle edges
                }
                
                float blendedValue = bary.x * p0.value + bary.y * p1.value + bary.z * p2.value;
                */
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
