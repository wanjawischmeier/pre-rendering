Shader "Unlit/TileDemoShader"
{
    Properties
    {
        _Scale ("Scale", Float) = 1.0
        _TileBlendFac ("Tile Blend Factor", Range(0, 1)) = 0.25
        _TexBlendFac ("Texture Blend Factor", Range(0, 1)) = 0.25
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
            float _Scale, _TileBlendFac, _TexBlendFac;

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

                // Calculate the tile index based on the UV coordinates
                int2 tileCoords = int2(uv * _TileBufferSize); // 64 tiles in each direction

                // Calculate the linear index for the tile buffer (assuming 64x64 tiles)
                int tileIndex = tileCoords.y * _TileBufferSize + tileCoords.x;

                // Fetch the TileResult from the buffer
                TileResult tile = _TileBuffer[tileIndex];
                fixed4 color = fixed4(1, 0, 0, 1);

                // Check if there are any valid texels in this tile (validCount > 0)
                if (tile.validCount > 0)
                {
                    // Use the first valid UV in the tile (index 0)
                    float2 uv = tile.validUV[0];

                    // Sample the texture at the valid UV coordinates (using _MainTex as example)
                    // color = fixed4(0, float(tile.validCount) / MAX_VALID_TEXELS, 0, 1);
                    color = fixed4(uv, float(tile.validCount) / MAX_VALID_TEXELS, 1);
                }
                
                fixed4 tileUVCol = fixed4(uv * _TileBufferSize % 1, 0, 1);
                color = lerp(color, tileUVCol, _TileBlendFac);
                return lerp(color, tex2D(_MainTex, uv).rrrr, _TexBlendFac);
            }
            ENDCG
        }
    }
}
