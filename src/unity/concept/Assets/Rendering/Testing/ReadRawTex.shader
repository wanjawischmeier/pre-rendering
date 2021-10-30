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

            uint2 unpack(uint v)
            {
                return uint2(v >> 16, v & 0xFFFF);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            int2 res;
            StructuredBuffer<uint> Tex;

            fixed4 frag(v2f i) : SV_Target
            {
                /*
                int2 tc = i.uv * res;
                uint c0 = Tex[tc.x + tc.y * res.y * 2];
                uint c1 = Tex[tc.x + tc.y * res.y * 2 +1];

                uint2 v0 = unpack(c0);
                uint2 v1 = unpack(c1);

                float4 col = float4(v0, v1) / 0xFFFF;

                return col;
                */
                return fixed4(0, 0, 0, 1);
            }
            ENDCG
        }
    }
}
