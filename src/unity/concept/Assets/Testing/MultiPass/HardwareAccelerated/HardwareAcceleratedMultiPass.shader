Shader"PreRendering/HardwareAcceleratedMultiPass"
{
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

    
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float depth : SV_Depth;
            };

            struct ShaderOutput
            {
                float4 color : COLOR;
                float depth : TEXCOORD0;
            };

            #define VALIDATION_ITERATIONS 1
            #define MARK_VERTEX_INVALID()   \
                o.pos = float4(0, 0, 0, 0); \
                o.uv = float3(0, 0, 0);     \
                return o;

            // propably violating the genova convention
            // the camera refused to render onto multiple slices, so this has to exist :/
            #define SAMPLE_PSEUDO_ARRAY(array, uv, slice, result)               \
                switch (slice) {                                                \
                    case 0:                                                     \
                        result = array##0.Sample(sampler_linear_repeat, uv);    \
                        break;                                                  \
                    case 1:                                                     \
                        result = array##1.Sample(sampler_linear_repeat, uv);    \
                        break;                                                  \
                    case 2:                                                     \
                        result = array##2.Sample(sampler_linear_repeat, uv);    \
                        break;                                                  \
                    case 3:                                                     \
                        result = array##3.Sample(sampler_linear_repeat, uv);    \
                        break;                                                  \
                    case 4:                                                     \
                        result = array##4.Sample(sampler_linear_repeat, uv);    \
                        break;                                                  \
                    case 5:                                                     \
                        result = array##5.Sample(sampler_linear_repeat, uv);    \
                        break;                                                  \
                    case 6:                                                     \
                        result = array##6.Sample(sampler_linear_repeat, uv);    \
                        break;                                                  \
                    case 7:                                                     \
                        result = array##7.Sample(sampler_linear_repeat, uv);    \
                        break;                                                  \
                    default:                                                    \
                        result = float4(1, 0, 1, 1);                            \
                        break;                                                  \
                }                

            SamplerState sampler_linear_repeat;
            Texture2D _Input0, _Input1, _Input2, _Input3, _Input4, _Input5, _Input6, _Input7;
            Texture2DArray<float4> _MotionVectors;
            StructuredBuffer<int> _Triangles;
            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float4> _UVs;
            
            uniform int DEBUG_MODE, RENDER_PASS;
            uniform float TIMESTEP, MAX_CIRCUMFERENCE, INTERPOLATION_RANGE;
            uniform float2 PROJECTION_RESOLUTION, MOTION_VECTOR_RESOLUTION;
            uniform uint _StartIndex;
            uniform uint _BaseVertexIndex;
            uniform float4x4 _ObjectToWorldMatricies[8];

            bool validLine(uint2 tc0, uint2 tc1, int slice)
            {
                // the line is fine (no component is greater than 1)
                if (!any(max(abs(int2(tc0) - int2(tc1)) - 1, 0)))
                {
                    return true;
                }
                
                #if VALIDATION_ITERATIONS == 1
                // use a single midpoint for fast approximation
                return _MotionVectors[uint3((tc0 + tc1) / 2, slice)].w != 0;
                
                #else
                // use a generalized iterative approach for accurate approximation
                uint3 tc = uint3(0, 0, slice);
                int2 direction = tc1 - tc0;
                float step = max(1, length(direction) / (VALIDATION_ITERATIONS + 2));
                direction = normalize(direction);

                // iterate over the points on the line
                [unroll(VALIDATION_ITERATIONS)]
                for (uint i = 1; i <= VALIDATION_ITERATIONS + 1; i++)
                {
                    // calculate the current texture coordinate
                    tc.xy = tc0 + round(i * step * direction);

                    // check if the current texture coordinate is valid
                    if (_MotionVectors[tc].w == 0)
                    {
                        return false;
                    }
                }
    
                return true;
                #endif
            }

            float4 interpolateColors(float4 color0, float4 color1, float blurriness0, float blurriness1)
            {
                float deltaBlurriness = blurriness0 - blurriness1;
                if (abs(deltaBlurriness) <= INTERPOLATION_RANGE)
                {
                    // interpolate between color0 and color1 based on the difference in blurriness values
                    float t = saturate(deltaBlurriness / INTERPOLATION_RANGE);
                    return lerp(color0, color1, t);
                }
                else
                {
                    // if the difference is outside the range, select the color with the least blurryness
                    return (blurriness0 < blurriness1) ? color0 : color1;
                }
            }

            v2f vert(appdata v)
            {
                v2f o;
                int index = _Triangles[v.vertexID + _StartIndex] + _BaseVertexIndex;
                /*
                // TODO: calculate indicies instead of sampling
                int index0 = v.vertexID - (v.vertexID % 3);     // i00
                int index1, index2;
                if ((index0 + 1) % 6)
                {
                    index1 = index0 + PROJECTION_RESOLUTION.x;  // i01
                    index2 = index1 + 1;                        // i11
                }
                else
                {
                    index1 = index0 + 1;                        // i10
                    index2 = index1 + PROJECTION_RESOLUTION.x;  // i11
                }
                */
                int baseIndex = v.vertexID - v.vertexID % 3;
                int offset = PROJECTION_RESOLUTION.x * PROJECTION_RESOLUTION.y;
                int index0 = _Triangles[baseIndex + 0 + _StartIndex] + _BaseVertexIndex;
                int index1 = _Triangles[baseIndex + 1 + _StartIndex] + _BaseVertexIndex;
                int index2 = _Triangles[baseIndex + 2 + _StartIndex] + _BaseVertexIndex;
    
                float4 uv0 = _UVs[index];
                float4 uv0_n0 = _UVs[index0];
                float4 uv0_n1 = _UVs[index1];
                float4 uv0_n2 = _UVs[index2];
                if (uv0_n0.w == -1 || uv0_n1.w == -1 || uv0_n2.w == -1)
                {
                    uv0.w = -1;
                }
    
                float4 uv1 = _UVs[index + offset];
                float4 uv1_n0 = _UVs[index0 + offset];
                float4 uv1_n1 = _UVs[index1 + offset];
                float4 uv1_n2 = _UVs[index2 + offset];
                if (uv1_n0.w == -1 || uv1_n1.w == -1 || uv1_n2.w == -1)
                {
                    uv1.w = -1;
                }
    
                if (uv0.w == -1 && uv1.w == -1)
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.uv0 = o.uv1 = uv0;
                    return o;
                }
                
                #if VALIDATION_ITERATIONS > 0
                if (RENDER_PASS == 0)
                {
                    uint2 tc0 = (uv0_n0.xy % 1) * MOTION_VECTOR_RESOLUTION;
                    uint2 tc1 = (uv0_n1.xy % 1) * MOTION_VECTOR_RESOLUTION;
                    uint2 tc2 = (uv0_n2.xy % 1) * MOTION_VECTOR_RESOLUTION;
                
                    if (!validLine(tc0, tc1, uv0.z) || !validLine(tc0, tc2, uv0.z))
                    {
                        o.pos = float4(0, 0, 0, 0);
                        o.uv0 = float4(0, 0, 0, -1);
                        return o;
                    }
                }
                #endif
                
                float3 pos = _Positions[index];
                float l0, l1, l2;
                if (RENDER_PASS == 0)
                {
                    float3 pos0 = _Positions[index0];
                    float3 pos1 = _Positions[index1];
                    float3 pos2 = _Positions[index2];
                    l0 = length(pos0 - pos1);
                    l1 = length(pos1 - pos2);
                    l2 = length(pos2 - pos0);
                    
                    // if (l0 > MAX_CIRCUMFERENCE || l1 > MAX_CIRCUMFERENCE || l2 > MAX_CIRCUMFERENCE)
                    if (l0 + l1 + l2 > MAX_CIRCUMFERENCE && false)
                    {
                        o.pos = float4(0, 0, 0, 0);
                        o.uv0 = o.uv1 = float4(0, 0, 0, -1);
                        return o;
                    }
        
                    uv0.z = l0 + l1 + l2;
                }
                /*
                if (uv0.x > 1)
                {
                    uv0.z = 1;
                    uv0.x %= 1;
                }
                else
                {
                    uv0.z = l0 + l1 + l2;
                }
                */
                if (uv0.z > MAX_CIRCUMFERENCE)
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.uv0 = o.uv1 = float4(0, 0, 0, -1);
                    return o;
                }
                
                float4 wpos = mul(_ObjectToWorldMatricies[uv0.w], float4(pos, 1.0f));
    
                if (DEBUG_MODE == 1 && RENDER_PASS != 0 && length(wpos.xyz - float3(-4, 0, -5)) < 20) // zSineFilled
                {
                    wpos.y += sin(TIMESTEP * 2) * sin(length(wpos)) * cos(sin(wpos.x * 10));
                }
    
                o.depth = length(wpos);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.uv0 = uv0;
                o.uv1 = uv1;
                // o.uv = RENDER_PASS == 0 ? uv0 : float3(0.5, 1, 0);
                /*
                if (uv0.w != uv0_n0.w || uv0.w != uv0_n1.w || uv0.w != uv0_n2.w)
                {
                    // not all verticies share texture index, interpolate colors
                    o.uv0 = float4(uv0.w < uv1.w, 0, 1, 1);
                }
                
                if (uv0.w != -1 && uv1.w != -1)
                {
                    if (uv0.w != uv0_n0.w || uv0.w != uv0_n1.w || uv0.w != uv0_n2.w)
                    {
                        o.uv0 = float4(uv0.w < uv1.w, 0, 1, 1);
                    }
                    if (uv1.w != uv1_n0.w || uv1.w != uv1_n1.w || uv1.w != uv1_n2.w)
                    {
                        o.uv1 = float4(uv0.w < uv1.w, 0, 1, 1);
                    }

                }
                */
                return o;
            }

            ShaderOutput frag(v2f i) : SV_Target
            {
                ShaderOutput o;
                if (RENDER_PASS == 0)
                {
                    o.color = float4(i.uv0.xyz, i.uv0.w + 1);
                }
                else if (true)
                {
                    o.color = i.uv0.z == -1 ? i.uv1.xyww : i.uv0.xyww;
                }
                else if(i.uv0.w == -1)
                {
                    SAMPLE_PSEUDO_ARRAY(_Input, i.uv1.xy, i.uv1.w, o.color);
                }
                else if(i.uv1.w == -1)
                {
                    SAMPLE_PSEUDO_ARRAY(_Input, i.uv0.xy, i.uv0.w, o.color);
                }   // TODO: extra cases (z0 < z1 etc.)
                else
                {
                    float4 col0, col1;
                    SAMPLE_PSEUDO_ARRAY(_Input, i.uv0.xy, i.uv0.w, col0);
                    SAMPLE_PSEUDO_ARRAY(_Input, i.uv1.xy, i.uv1.w, col1);
                    // o.color = interpolateColors(i.uv0.xyww, i.uv1.xyww, i.uv0.z, i.uv1.z);
                    // o.color = interpolateColors(col0, col1, i.uv0.z, i.uv1.z);
                    // o.color = i.uv0.z < i.uv1.z ? col0 : col1;
                    // o.color = i.uv0.z > i.uv1.z ? i.uv0.xyww : i.uv1.xyww;
                    o.color = i.uv0;
                    // o.color = i.uv0.zzzz / 10;
                }
    
                o.depth = i.depth;
                return o;
            }
            ENDCG
        }
    }
}
