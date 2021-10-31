Shader "Hidden/ProximityTest"
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

            fixed4 frag(v2f i) : SV_Target
            {
                float x = 2;
                float b = 0.5;
                float2 S = float2(0.5, 0.25);
                float2 C = float2(0.5, 0.5);

                float2 P = S + x * (C - S);
                float d = b * distance(P, i.uv) + (1 - b) * distance(C, i.uv);

                return d.xxxx;
            }
            ENDCG
        }
    }
}
