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
            #include "Macros.cginc"

            struct appdata
            {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

    
            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                bool differingSlice : TEXCOORD1;
                float depth : SV_Depth;
            };

            struct ShaderOutput
            {
                float4 color : SV_Target;
                float depth : SV_Depth;
            };

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
            uniform float4x4 _ObjectToWorldMatricies[MAX_SLICES];

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
    
                float4 uv1 = _UVs[index + offset];
                /*
                float4 uv1_n0 = _UVs[index0 + offset];
                float4 uv1_n1 = _UVs[index1 + offset];
                float4 uv1_n2 = _UVs[index2 + offset];
                if (uv1_n0.w == -1 || uv1_n1.w == -1 || uv1_n2.w == -1)
                {
                    uv1.w = -1;
                }
                */
                /*
                int slice = uv0.z;
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
                */
                float4 uv0 = _UVs[index];
                float3 pos = _Positions[index];
                float3 pos0 = _Positions[index0];
                float3 pos1 = _Positions[index1];
                float3 pos2 = _Positions[index2];
                float l0 = length(pos0 - pos1);
                float l1 = length(pos1 - pos2);
                float l2 = length(pos2 - pos0);
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
    
                if (RENDER_PASS == 0)
                {
                    uv0.z = l0 + l1 + l2;
        
                    if (l0 > MAX_CIRCUMFERENCE || l1 > MAX_CIRCUMFERENCE || l2 > MAX_CIRCUMFERENCE)
                    {
                        RETURN_INVALID_VERTEX();
                    }
                }
                else if (uv0.z > MAX_CIRCUMFERENCE)
                {
                    RETURN_INVALID_VERTEX();
                }
    
                int slice = uv0.w;
                if (RENDER_PASS == 0)
                {
                    o.differingSlice = false;
                }
                else
                {
                    float4 uv0_n0 = _UVs[index0];
                    float4 uv0_n1 = _UVs[index1];
                    float4 uv0_n2 = _UVs[index2];
                    if (uv0_n0.w == -1 || uv0_n1.w == -1 || uv0_n2.w == -1)
                    {
                        RETURN_INVALID_VERTEX();
                    }
        
                    if (slice != uv0_n0.w || slice != uv0_n1.w || slice != uv0_n2.w)
                    {
                        SAMPLE_PSEUDO_ARRAY(_Input, uv0.xy, slice, uv0);
                        o.differingSlice = true;
                    }
                    else
                    {
                        o.differingSlice = false;
                    }
                }
    
                float4 wpos = mul(_ObjectToWorldMatricies[slice], float4(pos, 1.0f));
    
                if (DEBUG_MODE == 1 && RENDER_PASS != 0 && length(wpos.xyz - float3(-4, 0, -5)) < 20) // zSineFilled
                {
                    wpos.y += sin(TIMESTEP * 2) * sin(length(wpos)) * cos(sin(wpos.x * 10));
                }
    
                o.depth = length(wpos);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.uv0 = uv0;
                return o;
            }

            ShaderOutput frag(v2f i) : SV_Target
            {
                ShaderOutput o;
                if (i.uv0.w == -1)
                {
                    o.color = float4(0, 0, 0, -1);
                }
                else if (RENDER_PASS == 0)
                {
                    o.color = float4(i.uv0.xyz, i.uv0.w);
                }
                else if (i.differingSlice)
                {
                    o.color = i.uv0;
                }
                else
                {
                    SAMPLE_PSEUDO_ARRAY(_Input, i.uv0.xy, i.uv0.w, o.color);
                }
                o.depth = i.depth;
                return o;
            }
            ENDCG
        }
    }
}
