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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            int2 res;
            StructuredBuffer<half4> Tex;

            fixed4 frag(v2f i) : SV_Target
            {
                int2 tc = i.uv * res;
                /*
                half4 c = half4(
                    Tex[tc.x + tc.y * res.y],
                    Tex[tc.x + tc.y * res.y +1],
                    Tex[tc.x + tc.y * res.y +2],
                    1
                );
                c.rgb /= (float)0xFFFF;
                */
                fixed4 c = fixed4(0, 0, 0, 1);
                half4 a = Tex[0];
                c.g = a.r == 0 ? 1 : 0;
                // fixed4 col = tex2D(_MainTex, i.uv);
                // col = c.r > 1 ? c : col;
                // col = c;
                // just invert the colors
                // col.rgb = 1 - c.rgb;
                return c;
            }
            ENDCG
        }
    }
}
