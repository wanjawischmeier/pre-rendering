Shader "Hidden/ReadUnpacked"
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

            ByteAddressBuffer InputBuffer;
            uint Address;
            int2 Resolution;

            fixed4 frag(v2f i) : SV_Target
            {
                uint offset = 0;
                int2 tc = i.uv * Resolution;
                // uint idx = (tc.x + (Resolution.y - tc.y - 0) * Resolution.x + offset) * Address;
                uint idx = (tc.x + (tc.y * Resolution.x)) - 3;
                uint3 val = InputBuffer.Load3(idx*3);
                // uint val = InputBuffer.Load(Address-3);
                
                fixed4 col;
                float max = 0xFFFFFFFF;
                // float max = Resolution.x * Resolution.y * 3;
                float3 r = (float3)val / float3(max, max, max);
                // float r = (float)val / max;
                col = fixed4(r, 1);
                // col = fixed4(r, r, r, 1);
                
                return col;
            }
            ENDCG
        }
    }
}
