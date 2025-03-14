Shader "Hidden/CubemapScreenBlit"
{
    Properties
    {
        _InputOutput ("Input Array", 2DArray) = "black" {}
        _MainTex ("Texture", 2D) = "white" {}
        _Contrast ("Contrast", Range(0, 2)) = 1
        _Brightness ("Brightness", Range(0, 1)) = 0.5
        _EdgeDetectionThreshold ("Edge Detection Threshold", Range(0, 1)) = 0.5
        _DownsampledMixThreshold ("Downsampled Mix Threshold", Range(-1, 1)) = 0.01
        [Toggle] _ShowIterative ("Show Iterative", Float) = 0
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
            Texture2DArray _Input, _FrontBufferFullRes, _FrontBufferDownsampled;
            Texture2DArray _FrontDepthBufferFullRes, _FrontDepthBufferIterative;
            Texture2DArray _FrontDepthBufferDownsampledLayer0, _FrontDepthBufferDownsampledLayer1;
            SamplerState point_clamp_sampler; // TODO: implement bilinear cubemap sampling
            SamplerState linear_clamp_sampler, linear_repeat_sampler;

            uniform int CLOSEST_CUBE_INDEX;
            uniform float _Contrast, _Brightness, _EdgeDetectionThreshold, _DownsampledMixThreshold, _ShowIterative;
            uniform float NCLIP, FCLIP, CAM_FCLIP;
            uniform float3 PLAYER_POSITION, OFFS;
            uniform int2 TARGET_RESOLUTION_DOWNSAMPLED;
            uniform float4 SORTED_CUBE_POSITIONS[10];
            uniform float4x4 _ViewToWorldMatrix, _InvProjectionMatrix;
            uniform float4x4 ORIENTATION_MATRICIES[6], INVERSE_ORIENTATION_MATRICIES[6];

            #include "Assets/Scripts/IterativeTransformUtility.cginc"
            #define DOWNSAMPLED_DEPTH_OFFSET 0.1    // TODO: change after proper depth calculation?
            #define NUM_DEPTH_TESTS 2               // expected to not be larger than cubemap count


            float4 GetColorFromDepth(float3 localPosition, float depth)
            {
                // reconstruct original world position and map it to original cubemap
                float3 worldPosition = normalize(localPosition) * depth;
                worldPosition += PLAYER_POSITION;

                float previousDepth = CAM_FCLIP;
                float3 tmpLocalPosition;
                float4 tmpColor, cubePosition;
                float4 uv, color = float4(0, 0, 0, 1);

                [unroll]
                for (int i = 0; i < 2; i++)
                {
                    cubePosition = SORTED_CUBE_POSITIONS[i];
                    localPosition = worldPosition - cubePosition.xyz;
                    
                    uv = WorldSpaceToCubemapUV(localPosition);
                    uv.w += CUBEMAP_FACE_COUNT * cubePosition.w;
                
                    tmpColor = _Input.Sample(linear_repeat_sampler, uv.xyw);
                    depth = tmpColor.a * (FCLIP - NCLIP) + NCLIP;

                    tmpLocalPosition = UVToWorldSpacePosition(uv.xy, depth, uv.w % CUBEMAP_FACE_COUNT, true);
                    tmpColor.a = length(tmpLocalPosition - localPosition); // store depth error in alpha channel

                    if (tmpColor.a < color.a)
                    {
                        // layer has less error than previous one
                        color = tmpColor;
                        previousDepth = depth;
                    }
                }

                return color;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // convert screen-space UV to NDC [-1, 1]
                float2 ndc = i.uv * 2 - 1;
                float4 clipPos = float4(ndc.xy, 1, 1);
                
                float4 viewPos = mul(_InvProjectionMatrix, clipPos);
                viewPos /= viewPos.w; // homogeneous divide

                // convert to local coordinates with z=1
                float3 localPosition = mul((float3x3)_ViewToWorldMatrix, viewPos.xyz);
                float4 cubemapUV = WorldSpaceToCubemapUV(localPosition);
                
                // return asfloat(_FrontDepthBufferFullRes.Sample(point_clamp_sampler, cubemapUV.xyw)).rrrr;
                // return asfloat(_FrontDepthBufferDownsampledLayer0.Sample(point_clamp_sampler, cubemapUV.xyw)).rrrr;
                float depthFullRes = asfloat(_FrontDepthBufferFullRes.Sample(linear_clamp_sampler, cubemapUV.xyw));
                float depthDownsampled = asfloat(_FrontDepthBufferDownsampledLayer0.Sample(linear_clamp_sampler, cubemapUV.xyw));
                // float depthDownsampled = asfloat(SampleDepthShaderBilinear(_FrontDepthBufferDownsampledLayer0, cubemapUV.xy, TARGET_RESOLUTION_DOWNSAMPLED, cubemapUV.w, FCLIP));
                // return depthFullRes;
                if (_ShowIterative == 1)
                {
                    depthFullRes = asfloat(_FrontDepthBufferIterative.Sample(linear_clamp_sampler, cubemapUV.xyw));
                }

                // return depth.rrrr;
                bool isDownsampled = false;
                float depth = depthFullRes;
                
                if (depthFullRes > 0)
                {
                    // full res depth is valid
                    /*
                    if (depthDownsampled > 0 && depthFullRes > depthDownsampled + _DownsampledMixThreshold)
                    {
                        // downsampled depth is valid and closer
                        depth = depthDownsampled;
                        isDownsampled = true;
                        // return float4(depthDownsampled, 0, 0, 1);
                        // return float4(0.5, 0, 0, 1);
                    }
                    else
                    {
                        // full res depth is closer (and already set)
                        // return float4(0, depthFullRes, 0, 1);
                        // return float4(0, 0.5, 0, 1);
                    }
                    */
                }
                else
                {
                    // check downsamled buffer
                    /*
                    if (depthDownsampled > 0)
                    {
                        // downsamled depth is valid
                        depth = depthDownsampled;
                        isDownsampled = true;
                        // return float4(depthDownsampled, 0, 0, 1);
                        // return float4(1, 0, 0, 1);
                    }
                    else
                    {
                        // no valid depth
                        return float4(1, 0, 1, 1);
                    }
                    */
                    depth = asfloat(_FrontDepthBufferIterative.Sample(linear_clamp_sampler, cubemapUV.xyw));
                }
                
                
                float4 color = GetColorFromDepth(localPosition, depth);

                /*
                // reconstruct original world position and map it to original cubemap
                float3 worldPosition = normalize(localPosition) * depth;
                worldPosition += PLAYER_POSITION;

                float previousDepth = CAM_FCLIP;
                float3 tmpLocalPosition;
                float4 tmpColor, cubePosition;
                float4 color = float4(0, 0, 0, 1);

                [unroll]
                for (int i = 0; i < 2; i++)
                {
                    cubePosition = SORTED_CUBE_POSITIONS[i];
                    localPosition = worldPosition - cubePosition.xyz;
                    
                    cubemapUV = WorldSpaceToCubemapUV(localPosition);
                    cubemapUV.w += CUBEMAP_FACE_COUNT * cubePosition.w;
                
                    tmpColor = _Input.Sample(linear_repeat_sampler, cubemapUV.xyw);
                    depth = tmpColor.a * (FCLIP - NCLIP) + NCLIP;

                    tmpLocalPosition = UVToWorldSpacePosition(cubemapUV.xy, depth, cubemapUV.w % CUBEMAP_FACE_COUNT);
                    tmpColor.a = length(tmpLocalPosition - localPosition); // store depth error in alpha channel

                    if (tmpColor.a < color.a - (isDownsampled ? 0 : 0))
                    {
                        // layer has less error than previous one
                        color = tmpColor;
                        previousDepth = depth;
                    }
                }
                */
                if (isDownsampled && color.a > _EdgeDetectionThreshold)
                {
                    // TODO: major error on downsampled edge detected, apply smooth edges
                    depth = asfloat(_FrontDepthBufferDownsampledLayer1.Sample(linear_clamp_sampler, cubemapUV.xyw));
                    // depth = asfloat(SampleDepthShaderBilinear(_FrontDepthBufferDownsampledLayer1, cubemapUV.xy, TARGET_RESOLUTION_DOWNSAMPLED, cubemapUV.w, FCLIP));
                    color = GetColorFromDepth(localPosition, depth);
                    
                    // return float4(depth, 0, 0, 1);
                    // return float4(1, 0, 0, 1);
                }


                // return worldPosition.xyzz;
                // return float4(CLOSEST_CUBE_INDEX, 0, float(cubemapUV.w) / 6, 1);
                if (isDownsampled)
                {
                    // color.r += 0.5;
                }

                return color;
                // return float4(oDepth, 0, isDownsampled ? 0.5 : 0, 1);



                // return color.aaaa;
                // return color.raaa * float4(1, 1, 0, 1);
                /*
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
                */
            }
            ENDCG
        }
    }
}
