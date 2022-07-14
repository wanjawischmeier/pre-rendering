Shader "Hidden/PostRasterization"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ProjTex ("Projection", 2D) = "white" {}
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

            sampler2D _MainTex, _ProjTex;
            float PI, PI2, FOV;
            int DEBUG;

            fixed4 frag (v2f i) : SV_Target
            {
                float x = PI2 * (i.uv.x - 0.5);
                float y = PI * (i.uv.y - 0.5);

                float p = sqrt(x * x + y * y);
                float c = atan2(p, FOV);

                float sinC = sin(c);

                // simplified gnomonic projection
                float phi = asin(y * sinC / p);
                float lambda = atan2(x * sinC, p * cos(c));

                float2 tc = float2(lambda / PI2 + 0.5, phi / PI + 0.5);
                tc = tc < 0 ? 1 - abs(tc) % 1 : tc % 1;

                fixed4 pc = tex2D(_ProjTex, tc);
                fixed4 col = tex2D(_MainTex, pc.xy);
                return DEBUG ? pc : col;
            }
            ENDCG
        }
    }
}
