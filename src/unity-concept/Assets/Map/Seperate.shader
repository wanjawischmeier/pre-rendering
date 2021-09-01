Shader "PreRendering/Seperate"
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

            // merges inputs with n bits each into one number with twice the bits
            int merge(int a, int b, int bits)
            {
                return (a << bits) | b;
            }

            // seperates the input into two numbers with n bits each
            int2 seperate(int c, int bits)
            {
                return int2(
                    c >> 4,
                    c & ((int) pow(2, bits) - 1)
                );
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 mps_comb = tex2D(_MainTex, i.uv);

                int trnsp_rough_comb = mps_comb.r * 0xF;
                int2 depth_sep = mps_comb.gb * 0xFF;

                int2 trnsp_rough = seperate(mps_comb.r * 0xF, 4);
                int depth = merge(depth_sep[0], depth_sep[1], 8);

                float4 out_col = float4(
                    trnsp_rough[0] / 0xF,
                    trnsp_rough[1] / 0xF,
                    depth / 0xFFFF,
                    1
                );

                return out_col;
            }
            ENDCG
        }
    }
}
