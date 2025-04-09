Shader "Unlit/TileDemoShader"
{
    Properties
    {
        _Scale ("Scale", Float) = 1.0
        _TileBlendFac ("Tile Blend Factor", Range(0, 1)) = 0.25
        _TexBlendFac ("Texture Blend Factor", Range(0, 1)) = 0.25
        _MaxUVDistance ("Max UV Distance", Float) = 0.1
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
            float _Scale, _TileBlendFac, _TexBlendFac, _MaxUVDistance;

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

                // Initialize variables for closest UV and distance
                float closestUVDistance = 1.0;
                float2 closestUV = float2(0, 0);

                // Check the current tile and its neighbors
                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
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
            }
            ENDCG
        }
    }
}
