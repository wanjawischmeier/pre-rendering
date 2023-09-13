Shader "Hidden/ComputeWorldSpacePosition"
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

            sampler2D _CameraDepthTexture;
            float4x4 _ViewProjInv;

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                if (depth == 0)
                {
                    return fixed4(0, 0, 0, 0);
                }
                float4 clipPos = float4(i.uv * 2.0 - 1.0, depth, 1.0);
                float4 worldPos = mul(_ViewProjInv, clipPos);
                float3 P = (worldPos / worldPos.w).xyz;
                
                return fixed4(P.xy, 1, depth);
            }
            ENDCG
        }
    }
}
