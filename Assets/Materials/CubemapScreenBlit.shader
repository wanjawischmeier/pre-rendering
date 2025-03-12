Shader "Hidden/CubemapScreenBlit"
{
    Properties
    {
        _InputOutput ("Input Array", 2DArray) = "black" {}
        _MainTex ("Texture", 2D) = "white" {}
        _Contrast ("Contrast", Range(0, 2)) = 1
        _Brightness ("Brightness", Range(0, 1)) = 0.5
        _DownsampledMixThreshold ("Downsampled Mix Threshold", Range(0, 1)) = 0.01
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

            sampler2D _MainTex;
            Texture2DArray _FrontBufferFullRes, _FrontBufferDownsampled, _FrontBufferDepth;
            SamplerState point_clamp_sampler; // TODO: implement bilinear cubemap sampling
            SamplerState linear_clamp_sampler;

            uniform float _Contrast, _Brightness, _DownsampledMixThreshold, FCLIP;
            uniform int2 INPUT_DOWNSAMPLED_RESOLUTION;
            uniform float4x4 _ViewToWorldMatrix, _InvProjectionMatrix;
            uniform float4x4 INVERSE_ORIENTATION_MATRICIES[6];

            #include "Assets/Scripts/IterativeTransformUtility.cginc"

            fixed4 frag (v2f i) : SV_Target
            {
                // convert screen-space UV to NDC [-1, 1]
                float2 ndc = i.uv * 2 - 1;
                float4 clipPos = float4(ndc.xy, 1, 1);
                
                float4 viewPos = mul(_InvProjectionMatrix, clipPos);
                viewPos /= viewPos.w; // homogeneous divide

                // convert to world-space direction
                float3 worldPosition = mul((float3x3)_ViewToWorldMatrix, viewPos.xyz);
                float4 cubemapUV = WorldSpaceToCubemapUV(worldPosition);

                // return asfloat(_FrontBufferDepth.Sample(point_clamp_sampler, cubemapUV.xyw).rrrr);
                float4 color = _FrontBufferFullRes.Sample(point_clamp_sampler, cubemapUV.xyw);
                // return color.aaaa;
                // return color.raaa * float4(1, 1, 0, 1);
                // float4 colorDownsampled = _FrontBufferDownsampled.Sample(linear_clamp_sampler, cubemapUV.xyw);
                float4 colorDownsampled = SampleShaderBilinear(_FrontBufferDownsampled, cubemapUV.xy, INPUT_DOWNSAMPLED_RESOLUTION, cubemapUV.w, FCLIP);
                if (color.a == 0)
                {
                    if (colorDownsampled.a == 0)
                    {
                        color = float4(1, 0, 1, 1);
                    }
                    else
                    {
                        color = colorDownsampled;
                        color.r += 0.5;
                    }
                }
                else if (colorDownsampled.a != 0 && colorDownsampled.a + _DownsampledMixThreshold < color.a)
                {
                    color = colorDownsampled;
                    color.r += 0.5;
                }

                color.rgb = (color.rgb % 1 - 0.5f) * _Contrast + _Brightness; // contrast

                float4 realtime = tex2D(_MainTex, i.uv);
                return realtime.a == 0 ? color : realtime; // TODO: implement depth sorting
            }
            ENDCG
        }
    }
}
