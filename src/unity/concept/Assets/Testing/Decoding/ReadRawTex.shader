Shader "Hidden/ReadRawTex"
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

            void unpack(uint v, out uint v0, out uint v1)
            {
                v0 = v & 0xFFFF;
                v1 = v >> 16;
            }

            half4 normalizeColor16b(uint r, uint g, uint b, uint a)
            {
                return half4(r, g, b, a) / (float)0xFFFF;
            }

            void normalizeByResolution(float2 v, float2 res, out float2 u)
            {
                u = v / res;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            StructuredBuffer<uint> RawTexture;
            uint2 Resolution;
            uint Offset;

            fixed4 frag(v2f i) : SV_Target
            {
                int2 tc = i.uv.xy * Resolution;
                int idx = (tc.x + (Resolution.y - tc.y - 1) * Resolution.x + Offset) * 2;

                uint bgPacked = RawTexture[idx];
                uint raPacked = RawTexture[idx + 1];

                uint r, g, b, a;

                unpack(bgPacked, b, g);
                unpack(raPacked, r, a);

                return normalizeColor16b(r, g, b, a);
            }

            ENDCG
        }
    }
}
