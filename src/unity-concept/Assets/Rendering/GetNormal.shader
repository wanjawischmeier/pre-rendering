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
            float depthSamples[9];
            float3 off = float3(-1, 0, 1);

            fixed4 frag(v2f i) : SV_Target
            {
                depthSamples[0] = tex2D(_MainTex, i.uv + float2(-normalOff, -normalOff)).a;
                depthSamples[1] = tex2D(_MainTex, i.uv + float2(0,          -normalOff)).a;
                depthSamples[2] = tex2D(_MainTex, i.uv + float2(normalOff,  -normalOff)).a;
                depthSamples[3] = tex2D(_MainTex, i.uv + float2(-normalOff, 0)).a;
                depthSamples[5] = tex2D(_MainTex, i.uv + float2(normalOff,  0)).a;
                depthSamples[6] = tex2D(_MainTex, i.uv + float2(-normalOff, normalOff)).a;
                depthSamples[7] = tex2D(_MainTex, i.uv + float2(0,          normalOff)).a;
                depthSamples[8] = tex2D(_MainTex, i.uv + float2(normalOff,  normalOff)).a;

                float2 normal = float2(
                    (depthSamples[2] + depthSamples[0] + 2 * (depthSamples[5] - depthSamples[3]) + depthSamples[8] - depthSamples[6]),
                    (depthSamples[6] + depthSamples[0] + 2 * (depthSamples[7] - depthSamples[1]) + depthSamples[8] - depthSamples[2])
                );
                // normal = normalize(normal);
                // normal = normal * 0.5 + 0.5;
                
                fixed4 col = fixed4(normal, 0, 1);

                return col;
            }
            ENDCG
        }
    }
}
