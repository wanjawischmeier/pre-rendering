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
            Texture2D<float4> _Coordinates;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 tc = _Coordinates[i.uv * (RESOLUTION - 1)];
                
                if (tc.a != 1)
                {
                    // TODO: sample skybox
                    return fixed4(0, 0, 0, 1);
                }
                
                float2 uv = tc.xy;
                fixed4 col = tex2D(_Input, uv);
                return col;
            }
            ENDCG
        }
    }
}
