Shader "Unlit/TileOccupancyDebug"
{
    Properties
    {
        _OutputResolution ("Output Resolution", Vector) = (1024, 1024, 0, 0)
        _TileSize ("Tile Size", Float) = 16
        _MaxVertsPerTile ("Max Verts Per Tile", Float) = 256
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            Texture2D<uint> _TileCounters;

            uniform uint2 _OutputResolution;
            uniform float _TileSize, _MaxVertsPerTile;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 HeatmapColor(float t)
            {
                // Green -> Yellow -> Red gradient
                float3 color;
                if (t < 0.5)
                    color = lerp(float3(0, 1, 0), float3(1, 1, 0), t * 2.0); // Green to Yellow
                else
                    color = lerp(float3(1, 1, 0), float3(1, 0, 0), (t - 0.5) * 2.0); // Yellow to Red
                return float4(color, 1);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv.y = 1 - uv.y; // Flip Y coordinate for correct UV mapping

                uint2 pixel = uv * _OutputResolution;
                uint2 tileCoord = pixel / _TileSize;

                int count = _TileCounters[tileCoord];
                if (count == 0)
                    return float4(0, 0, 0, 1); // Black for empty tiles
                else if (count >= _MaxVertsPerTile)
                    return float4(1, 0, 1, 1); // Pink warning for tile overflow

                float norm = saturate((float)count / _MaxVertsPerTile);
                return HeatmapColor(norm);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
