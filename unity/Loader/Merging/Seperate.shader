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

            // seperates the input into two numbers with n bits each
            uint2 seperate(uint c, uint bits)
            {
                return uint2(
                    c >> 4,
                    c & ((uint) pow(2, 4) -1)
                );
            }
            
            void UnpackFloat(float input, out float a, out float b) {

                //Unpacking
                uint uintInput = asuint(input);
                a = f16tof32(uintInput >> 16);
                b = f16tof32(uintInput);
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
                
                uint trnsp_rough_comb = mps_comb.r * 0xFF;

                uint2 trnsp_rough = seperate(trnsp_rough_comb, 4);

                float4 col = float4(
                    trnsp_rough[0] / 0xF,
                    trnsp_rough[1] / 0xF,
                    0,
                    1
                );

                return col;
            }
            ENDCG
        }
    }
}
