Shader "PreRendering/Merge"
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
                fixed4 mps = tex2D(_MainTex, i.uv);

                int trnsp = mps.r * 0xF;
                int rough = mps.g * 0xF;
                int depth = mps.b * 0xFFFF;

                int trnsp_rough = merge(trnsp, rough, 4);
                int2 depth_sep = seperate(depth, 8);

                float4 out_col = float4(
                    trnsp_rough,
                    depth_sep[0],
                    depth_sep[1],
                    1
                );

                out_col.rgb /= (float) 0xFF;

                /*
                int a = 5;
                int b = 13;
                int c = merge(a, b, 4);
                int2 r = seperate(c, 4);

                float4 out_col = float4(
                    a / (float) 0xF,
                    r[0] / (float) 0xF,
                    a / (float) 0xF == r[0] / (float) 0xF,
                    1
                );
                */

                return out_col;
            }
            ENDCG
        }
    }
}
