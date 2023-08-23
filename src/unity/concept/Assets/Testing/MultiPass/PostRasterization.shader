Shader "PreRendering/PostRasterization"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Off

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

            float2 RESOLUTION;
            sampler2D _Input;
            Texture2D<float2> _Coordinates;

            fixed4 frag (v2f i) : SV_Target
            {
                // still not working properly :(
                float2 uv = _Coordinates[i.uv * (RESOLUTION - 1)];
                float a = round(uv.x);
                
                if (uv.x == 0)
                {
                    // TODO: sample skybox
                    return fixed4(0, 0, 0, 1);
                }
                /*
                if (uv.x > 0)
                {
                    return fixed4(0, 1, 0, 1); // green
                }
                
                if (uv.x > 0)
                {
                    // pass
                }
                else
                {
                    return fixed4(0, 0, 1, 1); // blue
                }
                
                const float epsilon = 1000;
                if (a + 1000.0f >= 0.0f)
                {
                    return fixed4(0, 0, 1, 1); // blue
                }
                */
                // return fixed4(1, 0, 0, 1); // red
                
                // correct originally offset range
                uv -= 1;
                fixed4 col = tex2D(_Input, uv);
                // return fixed4(uv % 0.05 * (1 / 0.05), 0, 1);
                return col;
            }
            ENDCG
        }
    }
}
