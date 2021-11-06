Shader "Hidden/Packing"
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

            half2 unpack(uint v)
            {
                return uint2(v >> 16, v & 0xFFFF).yx / (float)0xFFFF;
            }

            half4 unpack(uint2 v)
            {
                return half4(unpack(v.x), unpack(v.y));
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            StructuredBuffer<uint2> Buff;
            int Idx;

            fixed4 frag (v2f i) : SV_Target
            {
                uint2 val = Buff[Idx];
                half4 col = unpack(val);

                return col;
            }
            ENDCG
        }
    }
}
