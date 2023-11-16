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
            
            float getDistancePointToViewRay(float3 p, float3 o, float3 d)
            {
                float numerator = length(cross(p - o, p - d));
                return numerator / length(d - o);
            }

            sampler2D _CameraDepthTexture;
            float3 PCAM, P0, P1, P2;
            float4x4 _ViewProjInv;

            fixed4 frag (v2f i) : SV_Target
            {
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                if (depth == 0)
                {
                    depth = 1;
                }
    
                float4 clipPos = float4(i.uv * 2.0 - 1.0, depth, 1.0);
                float4 worldPos = mul(_ViewProjInv, clipPos);
                float3 P = worldPos.xyz / worldPos.w;
                if (depth != 1)
                {
                    return fixed4(P, 1);
                }
    
                float d0 = getDistancePointToViewRay(P0, PCAM, P);
                float d1 = getDistancePointToViewRay(P1, PCAM, P);
                float d2 = getDistancePointToViewRay(P2, PCAM, P);
    
                if (d0 < d1 && d0 < d2)
                {
                    return fixed4(d0, 0, 0, 1);
                }
                else if (d1 < d0 && d1 < d2)
                {
                    return fixed4(0, d1, 0, 1);
                }
                else
                {
                    return fixed4(0, 0, d2, 1);
                }
            }
            ENDCG
        }
    }
}
