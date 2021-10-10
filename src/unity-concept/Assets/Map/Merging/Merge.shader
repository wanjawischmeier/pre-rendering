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
            uint merge(uint a, uint b, uint bits)
            {
                return (a << bits) | b;
            }
            
            float PackFloats(float a, float b) {

                //Packing
                uint a16 = f32tof16(a);
                uint b16 = f32tof16(b);
                uint abPacked = (a16 << 16) | b16;
                return asfloat(abPacked);
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
                float4 mps = tex2D(_MainTex, i.uv);
                mps *= pow(2, 3.2);

                float c = PackFloats(mps.r, mps.g);

                return float4(c / pow(2, 16), 0, 0, 1);
            }
            ENDCG
        }
    }
}
