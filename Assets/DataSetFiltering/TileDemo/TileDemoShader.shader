Shader "Unlit/TileDemoShader"
{
    Properties
    {
        _Scale ("Scale", Float) = 1.0
        _TileBlendFac ("Tile Blend Factor", Range(0, 1)) = 0.25
        _TexBlendFac ("Texture Blend Factor", Range(0, 1)) = 0.25
        _MaxUVDistance ("Max UV Distance", Range(0, 1)) = 0.1
        _BlendingEpsilon ("Blending Epsilon", Float) = 0.001
        _BlendingThreshold ("Blending Threshold", Range(0, 1)) = 0.001
        _TexelMarkerSize ("Texel Marker Size", Float) = 0.001
        [Toggle] _ShowMarkers ("Show Markers", Float) = 1.0
        _MaxBlendedTexels ("Max Blended Texels", Integer) = 3
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
            
            #define MAX_VALID_TEXELS 4

            struct TileResult
            {
                float2 validUV[MAX_VALID_TEXELS];
                uint validCount;
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

            sampler2D _MainTex;
            StructuredBuffer<TileResult> _TileBuffer;
            int _TileBufferSize;
            float _Scale, _TileBlendFac, _TexBlendFac, _MaxUVDistance, _BlendingEpsilon, _BlendingThreshold, _TexelMarkerSize;
            float _ShowMarkers;
            int _MaxBlendedTexels;

            // Find the closest valid data point
            float GetClosestDataValue(float2 uv, int2 tileCoords, out float2 closestUV)
            {
                float closestDist = 99999.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        int2 neighborCoords = (tileCoords + int2(x, y) + _TileBufferSize) % _TileBufferSize;
                        int tileIndex = neighborCoords.y * _TileBufferSize + neighborCoords.x;
                        TileResult tile = _TileBuffer[tileIndex];

                        for (uint i = 0; i < tile.validCount; i++)
                        {
                            float2 tileUV = tile.validUV[i];
                            float dist = distance(uv, tileUV);
                            if (dist < closestDist && dist <= _MaxUVDistance)
                            {
                                closestDist = dist;
                                closestUV = tileUV;
                            }
                        }
                    }
                }

                return closestDist;
            }

            // Blend values of nearby data points similar in value to the reference
            float BlendSimilarDataPoints(float2 uv, int2 tileCoords, float refValue)
            {
                float totalWeight = 0.0;
                float weightedValueSum = 0.0;
                int blendedTexels = 1;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        int2 neighborCoords = (tileCoords + int2(x, y) + _TileBufferSize) % _TileBufferSize;
                        int tileIndex = neighborCoords.y * _TileBufferSize + neighborCoords.x;
                        TileResult tile = _TileBuffer[tileIndex];

                        for (uint i = 0; i < tile.validCount; i++)
                        {
                            float2 tileUV = tile.validUV[i];
                            float value = tex2D(_MainTex, tileUV).r;
                            float valueDiff = abs(value - refValue);
                            if (valueDiff > _BlendingThreshold) continue;

                            float dist = distance(uv, tileUV);
                            if (dist > _MaxUVDistance) continue;
                            float weight = 1.0 / (dist + _BlendingEpsilon);

                            weightedValueSum += weight * value;
                            totalWeight += weight;
                            if (blendedTexels++ > _MaxBlendedTexels)
                            {
                                break;
                            }
                        }
                    }
                }

                return (totalWeight > 0.0) ? (weightedValueSum / totalWeight) : refValue;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv.xy * _Scale;

                // Calculate tile coords based on UV coordinates
                int2 tileCoords = int2(floor(uv * _TileBufferSize));

                float2 closestUV;
                float closestDist = GetClosestDataValue(uv, tileCoords, closestUV);
                if (_ShowMarkers == 1.0 && closestDist < _TexelMarkerSize)
                {
                    return float4(1, 0.5, 0, 1);  // Grayscale output
                }
                
                float closestValue = tex2D(_MainTex, closestUV).r;
                float blendedValue = BlendSimilarDataPoints(uv, tileCoords, closestValue);
                return blendedValue.rrrr;

                /*
                // Initialize variables for closest UV and distance
                float closestUVDistance = 1.0;
                float2 closestUV = float2(0, 0);

                // Check the current tile and its neighbors
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        int2 neighborCoords = (tileCoords + int2(x, y) + _TileBufferSize) % _TileBufferSize;
                        
                        // Calculate the linear index for the tile buffer
                        int tileIndex = neighborCoords.y * _TileBufferSize + neighborCoords.x;

                        // Fetch the TileResult from the buffer
                        TileResult tile = _TileBuffer[tileIndex];

                        // Check if there are any valid texels in this tile (validCount > 0)
                        if (tile.validCount > 0)
                        {
                            float uvDistance;
                            float2 tmp;

                            [unroll]
                            for (uint i = 0; i < MAX_VALID_TEXELS; i++)
                            {
                                if (i >= tile.validCount)
                                    break;

                                tmp = tile.validUV[i];
                                uvDistance = length(uv - tmp);

                                if (uvDistance < _MaxUVDistance && uvDistance < closestUVDistance)
                                {
                                    closestUV = tmp;
                                    closestUVDistance = uvDistance;
                                }
                            }
                        }
                    }
                }

                if (closestUVDistance == 1.0)
                {
                    // No valid texels found, return a default color
                    return fixed4(0.1, 0, 0, 1);
                }

                fixed4 sampled = tex2D(_MainTex, closestUV).rrrr;
                fixed4 color = fixed4(closestUVDistance * 2 * _TileBufferSize.xxx, 1);
                return sampled;
                fixed4 tileUVCol = fixed4(floor(uv * _TileBufferSize) % 1, 0, 1);
                color = lerp(color, tileUVCol, _TileBlendFac);
                return lerp(color, sampled, _TexBlendFac);
                */
            }
            ENDCG
        }
    }
}
