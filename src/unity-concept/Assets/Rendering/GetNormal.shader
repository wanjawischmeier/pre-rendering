Shader "Hidden/GetNormal"
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
            float2 Resolution;
            float2 size = { 2.0,0.0 };
            float3 off = { -1.0,0.0,1.0 };

            float3 filterNormal(sampler2D tex, float2 uv, float2 res)
            {
                float2 texelSize = 1.0 / res;

                float4 h;
                h[0] = tex2D(tex, uv + texelSize * float2(0, -1)).a;
                h[1] = tex2D(tex, uv + texelSize * float2(-1, 0)).a;
                h[2] = tex2D(tex, uv + texelSize * float2(1, 0)).a;
                h[3] = tex2D(tex, uv + texelSize * float2(0, 1)).a;

                float3 n;
                n.z = h[0] - h[3];
                n.x = h[1] - h[2];
                n.y = 0;

                return normalize(n);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = filterNormal(_MainTex, i.uv, Resolution);
                return fixed4(normal, 1.0);
            }
            ENDCG
        }
    }
}
