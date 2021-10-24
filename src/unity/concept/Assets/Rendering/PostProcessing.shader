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

            float2 calculateNormals(Texture2DArray _Input, SamplerState sampler_Input, float2 tc, float2 texelSize, float index)
            {
                float s0 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(-texelSize.x, -texelSize.y), index)).a;
                float s1 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(0,            -texelSize.y), index)).a;
                float s2 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(texelSize.x,  -texelSize.y), index)).a;
                float s3 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(-texelSize.x, 0           ), index)).a;
                float s5 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(texelSize.x,  0           ), index)).a;
                float s6 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(-texelSize.x, texelSize.y ), index)).a;
                float s7 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(0,            texelSize.y ), index)).a;
                float s8 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(texelSize.x,  texelSize.y ), index)).a;

                float b = 2;
                float2 n = float2(
                    -(s2 - s0 + 2 * (s5 - s3) + s8 - s6),
                    -(s6 - s0 + 2 * (s7 - s1) + s8 - s2)
                    );

                return normalize(n) * 0.5 + 0.5;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            Texture2D<half4> _Projection;
            Texture2DArray<float4> _InputArray;
            SamplerState linear_repeat_sampler;
            SamplerState sampler_InputArray;
            float FOV, FCLIP, CUTOFF;
            float2 Rotation, InputArrayRes;
            int Debug, MX_IDX;

            fixed4 frag (v2f i) : SV_Target
            {
                float2 tc = gnomonicProjection(i.uv, FOV, Rotation.x, Rotation.y);
                half4 idx = _Projection.Sample(linear_repeat_sampler, tc);

                idx.z *= MX_IDX;
                idx.z -= 1;

                float2 texelSize = 1 / InputArrayRes;
                float2 n = calculateNormals(_InputArray, sampler_InputArray, idx.xy, texelSize, idx.z);

                switch(Debug)
                {
                case 1:
                    return fixed4(idx.xy, 0, 1);
                case 2:
                    return fixed4(n, 0, 1);
                }
                
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_InputArray, idx.xyz);
                
                return col;
            }
            ENDCG
        }
    }
}
