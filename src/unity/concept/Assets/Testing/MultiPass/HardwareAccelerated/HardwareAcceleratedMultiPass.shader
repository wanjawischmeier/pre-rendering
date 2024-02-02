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
                float4 uv : TEXCOORD0;
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
                int index0 = _Triangles[baseIndex + 0 + _StartIndex] + _BaseVertexIndex;
                int index1 = _Triangles[baseIndex + 1 + _StartIndex] + _BaseVertexIndex;
                int index2 = _Triangles[baseIndex + 2 + _StartIndex] + _BaseVertexIndex;
                
                o.uv = _UVs[index];
                int slice0, slice1, slice2, slice = o.uv.w;
    
                if (RENDER_PASS == 0)
                {
                    o.differingSlice = false;
                }
                else
                {
                    float4 uv_n0 = _UVs[index0];
                    float4 uv_n1 = _UVs[index1];
                    float4 uv_n2 = _UVs[index2];
                    slice0 = uv_n0.w;
                    slice1 = uv_n1.w;
                    slice2 = uv_n2.w;
                    
                    if (slice0 == -1 || slice1 == -1 || slice2 == -1)
                    {
                        RETURN_INVALID_VERTEX();
                    }
        
                    if (slice0 != slice || slice1 != slice || slice2 != slice)
                    {
                        SAMPLE_PSEUDO_ARRAY(_Input, o.uv.xy, slice, o.uv);
                        o.differingSlice = true;
                    }
                    else
                    {
                        o.differingSlice = false;
                    }
        
                #if VALIDATION_ITERATIONS > 0
                    uint2 tc0 = (uv_n0.xy % 1) * MOTION_VECTOR_RESOLUTION;
                    uint2 tc1 = (uv_n1.xy % 1) * MOTION_VECTOR_RESOLUTION;
                    uint2 tc2 = (uv_n2.xy % 1) * MOTION_VECTOR_RESOLUTION;
                
                    if (!validLine(tc0, tc1, slice) || !validLine(tc0, tc2, slice))
                    {
                        RETURN_INVALID_VERTEX();
                    }
                #endif
                }
    
                float3 pos = _Positions[index];
                float3 pos0 = _Positions[index0];
                float3 pos1 = _Positions[index1];
                float3 pos2 = _Positions[index2];
    
                if (RENDER_PASS != 0)
                {
                    pos0 = mul(_ObjectToWorldMatricies[slice0], float4(pos0, 1.0f));
                    pos1 = mul(_ObjectToWorldMatricies[slice1], float4(pos1, 1.0f));
                    pos2 = mul(_ObjectToWorldMatricies[slice2], float4(pos2, 1.0f));
                }
    
                float l0 = length(pos0 - pos1);
                float l1 = length(pos1 - pos2);
                float l2 = length(pos2 - pos0);
    
                if (RENDER_PASS == 0)
                {
                    o.uv.z = l0 + l1 + l2;
                }
        
                if (l0 > MAX_CIRCUMFERENCE || l1 > MAX_CIRCUMFERENCE || l2 > MAX_CIRCUMFERENCE)
                {
                    RETURN_INVALID_VERTEX();
                }
                
                // float4 wpos = mul(_ObjectToWorldMatricies[slice], float4(pos, 1.0f));
                float4 wpos = mul(_ObjectToWorldMatricies[0], float4(pos, 1.0f));
                // float4 wpos = pos;
                // float4 wpos = float4(pos, 1.0f);
                if (DEBUG_MODE == 1 && RENDER_PASS != 0) // zSineFilled
                {
                    wpos.y += sin(TIMESTEP) * sin(length(wpos) * 8) * (4 / length(wpos));
                }
    
                o.depth = length(wpos);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                return o;
            }

            ShaderOutput frag(v2f i) : SV_Target
            {
                ShaderOutput o;
                if (i.uv.w == -1) // TODO: condition redundant?
                {
                    o.color = float4(0, 0, 0, 0);
                }
                else if (RENDER_PASS == 0 || DEBUG_MODE == 5 || i.differingSlice)
                {
                    o.color = i.uv;
                }
                else if (DEBUG_MODE == 2)
                {
                    o.color = float4(i.uv.zzz, 1);
                }
                else
                {
                    SAMPLE_PSEUDO_ARRAY(_Input, i.uv.xy, i.uv.w, o.color);
                }
                o.depth = i.depth;
                return o;
            }
            ENDCG
        }
    }
}
