Shader"PreRendering/PostRasterization"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
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
            
            #define MAX_SLICES 4
            #define DEPTH_TOLERANCE 0.0001

            uniform int NUM_SLICES, DEBUG_MODE, SLICE, MAX_CIRCUMFERENCE;
            uniform float INTERPOLATION_RANGE, DEPTH_OFFSET;
            uniform float2 RESOLUTION;

            sampler2D _MainTex;
            sampler2D _Input0, _Input1, _Input2, _Input3;
            sampler2D _Coordinates0, _Coordinates1, _Coordinates2, _Coordinates3;
            sampler2D _Depth0, _Depth1, _Depth2, _Depth3;
            
            // propably violating the genova convention
            // the camera refused to render onto multiple slices, so this has to exist :/
            #define SAMPLE_PSEUDO_ARRAY(array, uv, slice, result)   \
                switch (slice) {                                    \
                    case 0:                                         \
                        result = tex2D(array##0, uv);               \
                        break;                                      \
                    case 1:                                         \
                        result = tex2D(array##1, uv);               \
                        break;                                      \
                    case 2:                                         \
                        result = tex2D(array##2, uv);               \
                        break;                                      \
                    case 3:                                         \
                        result = tex2D(array##3, uv);               \
                        break;                                      \
                    default:                                        \
                        result = float4(1, 0, 1, 1);                \
                        break;                                      \
                }

            float4 interpolateColors(float4 color0, float4 color1, float blurriness0, float blurriness1)
            {
                float deltaBlurriness = blurriness0 - blurriness1;
                if (abs(deltaBlurriness) <= INTERPOLATION_RANGE)
                {
                    // interpolate between color0 and color1 based on the difference in blurriness values
                    float t = saturate(deltaBlurriness / INTERPOLATION_RANGE);
                    // return t.xxxx;
                    return lerp(color0, color1, t);
                }
                else
                {
                    // if the difference is outside the range, select the color with the least blurryness
                    return (blurriness0 < blurriness1) ? color0 : color1;
                }
            }

            void sampleLeastBlurrySlices(float2 uv, out float4 slice0, out float4 slice1, out float d)
            {
                float4 tc;
                float d0, d1 = 0;
    
                // initialize slices with high invalid blurryness
                slice0 = float4(0, 0, MAX_CIRCUMFERENCE, 0);
                slice1 = float4(0, 0, MAX_CIRCUMFERENCE, 0);

                for (int slice = 0; slice < NUM_SLICES; slice++)
                {
                    SAMPLE_PSEUDO_ARRAY(_Coordinates, uv, slice, tc);
                    
                    // check if the slice is valid
                    if (tc.w >= 1)
                    {
                        SAMPLE_PSEUDO_ARRAY(_Depth, uv, slice, d0);
                        
                        // sort using depth layers
                        if (d0 > d1 + DEPTH_TOLERANCE)
                        {
                            // new closest layer, clear least blurry slices
                            slice0 = tc;
                            slice1 = float4(0, 0, MAX_CIRCUMFERENCE, 0);
                            d1 = d0;
                            d = d0;
                        }
                        else if (d0 > d1 - DEPTH_TOLERANCE)
                        {
                            // pixel is in current layer, compare blurryness values
                            if (tc.z < slice0.z)
                            {
                                slice1 = slice0;
                                slice0 = tc;
                            }
                            else if (tc.z < slice1.z)
                            {
                                slice1 = tc;
                            }
                        }
                    }
                }
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // float2 uv = tex2D(_Coordinates0, i.uv);
                // return tex2D(_Input0, uv);
                float d;
                float4 col, col0, col1, slice0, slice1;
                sampleLeastBlurrySlices(i.uv, slice0, slice1, d);
    
                // correct initial texture index offset
                int index0 = slice0.w - 1;
                int index1 = slice1.w - 1;
    
                bool sliceValid0 = index0 >= 0;
                bool sliceValid1 = index1 >= 0;
                // return (d * 3).xxxx;
                // return fixed4(slice0.w / 2, slice1.w / 2, 0, 1);
                
                if (sliceValid0)
                {
                    SAMPLE_PSEUDO_ARRAY(_Input, slice0.xy, index0, col0);
                    sliceValid0 = sliceValid0 && col0.a < 1;
                }
                if (sliceValid1)
                {
                    SAMPLE_PSEUDO_ARRAY(_Input, slice1.xy, index1, col1);
                    sliceValid1 = sliceValid1 && col1.a < 1;
                }
    
                if (DEBUG_MODE == 2)
                {
                    return slice0.bbba * float4(index0, 1 - index0, 0, 1);
                }
    
                if (sliceValid0 && !sliceValid1)
                {
                    // only slice0 is valid, sample its color
                    col = col0;
                }
                else if (!sliceValid0 && sliceValid1)
                {
                    // only slice1 is valid, sample its color
                    col = col1;
                }
                else if (sliceValid0 && sliceValid1)
                {
                    // both slices are valid, interpolate between them
                    if (slice0.w < slice1.w)
                    {
                        col = interpolateColors(col0, col1, slice0.z, slice1.z);
                    }
                    else
                    {
                        col = interpolateColors(col1, col0, slice1.z, slice0.z);
                    }
                    // col = fixed4(0, 1, 0, 1);
                }
                else
                {
                    // no slice is valid, sample skybox / urp render
                    col = tex2D(_MainTex, i.uv);
                }
    
                // return tex2D(_Depth1, i.uv);
                return col;
                
            }
            ENDCG
        }
    }
}
