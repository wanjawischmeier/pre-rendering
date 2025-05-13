Shader "Unlit/VertexLookupDebug"
{
    Properties {}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            Texture2D<int2> _VertexLookup;

            float2 _Resolution;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                uint2 tc = floor(i.uv * _Resolution);
                int2 lookup = _VertexLookup[tc];

                uint2 tc_p;
                tc_p.x = lookup.x % _Resolution.x;
                tc_p.y = (lookup.x - tc_p.x) / _Resolution.x;

                float2 uv_p = float2(tc_p) / _Resolution;
                float depth = asfloat(lookup.y);

                return float4(uv_p, depth, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
