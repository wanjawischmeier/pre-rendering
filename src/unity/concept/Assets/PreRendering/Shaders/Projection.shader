Shader "PreRendering/Projection"
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
            #include "ProjectionHelper.cginc"
            #include "RawSampler.cginc"

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

            int IMG_IDX, MX_IDX;
            float NCLIP, FCLIP;
            float2 InputArrayRes, ProjectionRes, ProjectedRes;
            float3 Position, PositionOffset;
            Texture2DArray<half4> _InputArray;

            const float2 size = float2(2.0, 0.0);
            const float3 off = float3(-1, 0, 1);

            fixed4 frag (v2f i) : SV_Target
            {
                float3 inpIdx = float3(i.uv * InputArrayRes, IMG_IDX);
                float2 ll1 = normalizedToLatLon(i.uv.yx);
    
                float CP = _InputArray[inpIdx].a * (FCLIP - NCLIP) + NCLIP;
                float2 ll2 = translateLatLon(ll1, Position - PositionOffset, CP);
                ll2 = latLonToNormalized(ll2);
                
                return half4(ll2, (IMG_IDX + 1) / (float) MX_IDX, 1);
            }
            ENDCG
        }
    }
}
