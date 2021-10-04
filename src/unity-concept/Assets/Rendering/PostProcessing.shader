Shader "PreRendering/PostProcessing"
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
            #include "Shading.cginc"

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
            
            float PI, PI2;

            float2 gnomonicProjection(float2 pos, float fov, float phi1, float lambda0)
            {
                float x = PI2 * (pos.x - 0.5);
                float y = PI * (pos.y - 0.5);

                float p = sqrt(x * x + y * y);
                float c = atan2(p, fov);

                float sinC = sin(c); float cosC = cos(c);
                float sinPhi1 = sin(phi1); float cosPhi1 = cos(phi1);

                float phi = asin(cosC * sinPhi1 + y * sinC * cosPhi1 / p);
                float lambda = lambda0 + atan2(x * sinC, (p * cosPhi1 * cosC - y * sinPhi1 * sinC));

                return float2(lambda / PI2 + 0.5, phi / PI + 0.5);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            Texture2DArray<float4> _Input;
            Texture2D<float4> _Projected;
            SamplerState bilinear_repeat_sampler;
            SamplerState sampler_Input;
            float FOV;
            float2 Rotation;
            int Debug;

            fixed4 frag (v2f i) : SV_Target
            {
                float2 tc = gnomonicProjection(i.uv, FOV, Rotation.x, Rotation.y);
                // tc = tc < 0 ? 1 - abs(tc) % 1 : tc % 1;

                float4 idx = _Projected.Sample(bilinear_repeat_sampler, tc);

                if (Debug) return idx;
                
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_Input, idx.xyz);
                
                return col;
            }
            ENDCG
        }
    }
}
