Shader "Hidden/ReadUnpacked"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            ByteAddressBuffer InputBuffer;
            uint Address, Comp;
            int2 Resolution;

            fixed4 frag(v2f i) : SV_Target
            {
                uint offset = 0;
                int2 tc = i.uv * Resolution;
                // int idx = (tc.x + (Resolution.y - tc.y - 0) * Resolution.x + offset) * Address;
                int idx = tc.x + tc.y * Resolution.x;
                // uint3 val = InputBuffer.Load3(idx * 3);
                // uint val = InputBuffer.Load(Address-3);

                uint val = InputBuffer.Load(idx*3);
                uint r = val & 0xFF;
                uint g = (val >> 8) & 0xFF;
                uint b = (val >> 16) & 0xFF;
                
                // fixed4 col = fixed4(0, 0, 0, 1);
                fixed4 col = fixed4(
                    val & 0xFF,
                    (val >> 8) & 0xFF,
                    (val >> 16) & 0xFF,
                    0xFF
                ) / (fixed)0xFF;
                /*
                if (r == 5) col.r = 1;
                if (g == 6) col.g = 1;
                if (b == 7) col.b = 1;
                */
                /*
                float max = 0xFFFFFFFF;
                // float max = Resolution.x * Resolution.y * 3;
                // float3 r = (float3)val / float3(max, max, max);
                float r = (float)val / (float)100;
                // col = fixed4(r, 1);
                col = fixed4(r, r, r, 1);

                if (val < 0)
                    col = fixed4(1, 1, 0, 1);
                if (val > asint(0xFF))
                    col = fixed4(1, 0, 0, 1);

                if (val < Comp)
                    col = fixed4(1, 0, 1, 1);
                if (val == Comp)
                    col = fixed4(0, 1, 0, 1);
                */
                return col;
            }
            ENDCG
        }
    }
}
