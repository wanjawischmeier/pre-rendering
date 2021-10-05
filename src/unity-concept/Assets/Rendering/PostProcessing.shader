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
            #pragma multi_compile PERCISION_LOW PERCISION_HIGH

            #include "UnityCG.cginc"
            #include "Shading.cginc"
            // #include "Assets/Tests.cs"

            #if PERCISION_LOW
            typedef half  num;
            typedef half1 num1;
            typedef half2 num2;
            typedef half3 num3;
            typedef half4 num4;
            #elif PERCISION_HIGH
            typedef float  num;
            typedef float1 num1;
            typedef float2 num2;
            typedef float3 num3;
            typedef float4 num4;
            #endif

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

            Texture2DArray<float4> _InputArray;
            Texture2D<num4> _Projected;
            SamplerState bilinear_repeat_sampler;
            SamplerState sampler_InputArray;
            float FOV, FCLIP;
            float2 Rotation;
            int Debug, MX_IDX;

            fixed4 frag (v2f i) : SV_Target
            {
                float2 tc = gnomonicProjection(i.uv, FOV, Rotation.x, Rotation.y);
                num4 idx = _Projected.Sample(bilinear_repeat_sampler, tc);
                idx.z *= MX_IDX;
                idx.z += 1;
                idx.w *= FCLIP;

                if (idx.z < (MX_IDX +1) / (float) MX_IDX -1) idx = float4(tc, 0, FCLIP);

                if (Debug) return idx;
                
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_InputArray, idx.xyz);
                
                return col;
            }
            ENDCG
        }
    }
}
