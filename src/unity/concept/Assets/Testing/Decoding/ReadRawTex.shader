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
            #include "Assets/PreRendering/Shaders/RawSampler.cginc"

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
            StructuredBuffer<uint> RawTexture;
            uint2 Resolution;
            uint TextureOffset;

            fixed4 frag(v2f i) : SV_Target
            {
                half4 col = rawTex2D(RawTexture, i.uv, Resolution, TextureOffset);
                return col;
            }

            ENDCG
        }
    }
}
