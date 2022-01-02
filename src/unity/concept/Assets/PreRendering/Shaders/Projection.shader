Shader "PreRendering/Projection"
{
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
            #include "Helper.cginc"

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

            UNITY_DECLARE_TEX2DARRAY(_InputBuffer);
            int IMG_IDX, MX_IDX;
            float NCLIP, FCLIP;
            float2 ProjectionRes, ProjectedResolution;
            float3 Position, PositionOffset;

            const float2 Size = float2(2.0, 0.0);

            fixed4 frag (v2f i) : SV_Target
            {
                float2 ll1 = normalizedToLatLon(i.uv.yx);
    
                float CP = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(i.uv, IMG_IDX));
                CP *= (FCLIP - NCLIP) + NCLIP;

                float2 ll2 = translateLatLon(ll1, Position - PositionOffset, CP);
                ll2 = latLonToNormalized(ll2);
                
                return half4(ll2, (IMG_IDX + 1) / (float) MX_IDX, 1);
            }
            ENDCG
        }
    }
}
