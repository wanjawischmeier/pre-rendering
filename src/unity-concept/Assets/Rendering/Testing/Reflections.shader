Shader "Hidden/Reflections"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NormalTex ("Normal", 2D) = "white" {}
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
            sampler2D _NormalTex;
            float2 Resolution;
            float PI;

            fixed4 frag(v2f i) : SV_Target
            {
                // float3 col = tex2D(_MainTex, i.uv).rgb;
                // float3 col = tex2D(_NormalTex, i.uv).rgb;
                float2 rad = float2(PI, PI * 2);
                float2 latlon = i.uv.yx * rad;
                float2 inputAngle = latlon + rad / 2.0;
                float3 normal = tex2D(_NormalTex, i.uv).rgb;
                // normalAngle = float3

                return fixed4(inputAngle, 0, 1);
                // return fixed4(i.uv.xx, 0, 1);
                // return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
