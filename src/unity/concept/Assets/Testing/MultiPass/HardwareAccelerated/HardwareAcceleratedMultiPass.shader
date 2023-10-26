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
                float2 uv : TEXCOORD0;
                float perimeter : TEXCOORD1;
                float depth : SV_Depth;
            };

            struct ShaderOutput
            {
                half4 color : COLOR;
                half depth : TEXCOORD0;
            };

            #define VALIDATION_ITERATIONS 1
            #define AREA_TRIANGLE_SQ(v0, v1, v2) \
                (pow(length(v0 - v1), 2) + pow(length(v1 - v2), 2) + pow(length(v2 - v0), 2)) * 1000

            Texture2DArray<float4> _MotionVectors;
            StructuredBuffer<int> _Triangles;
            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float2> _UVs;
            
            uniform int DEBUG_MODE, RENDER_PASS, TEXTURE_INDEX;
            uniform float TIMESTEP, MAX_CIRCUMFERENCE;
            uniform float2 PROJECTION_RESOLUTION, INPUT_RESOLUTION;
            uniform uint _StartIndex;
            uniform uint _BaseVertexIndex;
            uniform float4x4 _ObjectToWorld;

            bool validLine(uint2 tc0, uint2 tc1)
            {
                // the line is fine (no component is greater than 1)
                if (!any(max(abs(tc0 - tc1) - 1, 0)))
                {
                    return true;
                }
                
                #if VALIDATION_ITERATIONS == 1
                // use a single midpoint for fast approximation
                return _MotionVectors[uint3((tc0 + tc1) / 2, TEXTURE_INDEX)].w != 0;
                
                #else
                // use a generalized iterative approach for accurate approximation
                uint3 tc = uint3(0, 0, TEXTURE_INDEX);
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
                
                // TODO: calculate indicies instead of sampling
                /*
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
    
                float2 uv = _UVs[index];
                float2 uv0 = _UVs[index0];
                float2 uv1 = _UVs[index1];
                float2 uv2 = _UVs[index2];
                if (uv0.x == -1 || uv1.x == -1 || uv2.x == -1)
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.uv = float2(0, 0);
                    return o;
                }
                
                #if VALIDATION_ITERATIONS > 0
                uint2 tc0 = (uv0 % 1) * INPUT_RESOLUTION;
                uint2 tc1 = (uv1 % 1) * INPUT_RESOLUTION;
                uint2 tc2 = (uv2 % 1) * INPUT_RESOLUTION;
    
                if (!validLine(tc0, tc1) || !validLine(tc0, tc2))
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.uv = float2(0, 0);
                    return o;
                }
                #endif
                
                float3 pos = _Positions[index];
                float3 pos0 = _Positions[index0];
                float3 pos1 = _Positions[index1];
                float3 pos2 = _Positions[index2];
                float l0 = length(pos0 - pos1);
                float l1 = length(pos1 - pos2);
                float l2 = length(pos2 - pos0);
                o.perimeter = l0 + l1 + l2;
    
                if (uv.x > 1)
                {
                    o.perimeter = 1;
                    uv %= 1;
                }
                else
                {
                    o.perimeter = l0 + l1 + l2;
                }
    
                if (l0 > MAX_CIRCUMFERENCE || l1 > MAX_CIRCUMFERENCE || l2 > MAX_CIRCUMFERENCE)
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.uv = float2(0, 0);
                    return o;
                }
    
                float4 wpos = mul(_ObjectToWorld, float4(pos, 1.0f));
    
                if (DEBUG_MODE == 1 && RENDER_PASS != 0 && length(wpos.xyz - float3(-4, 0, -5)) < 10) // zSineFilled
                {
                    wpos.y += sin(TIMESTEP * 2) * sin(length(wpos)) * sin(wpos.x);
                }
    
                o.depth = length(wpos);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.uv = uv;
                return o;
            }

            ShaderOutput frag(v2f i) : SV_Target
            {
                ShaderOutput o;
                o.color = float4(i.uv, i.perimeter, TEXTURE_INDEX + 1);
                o.depth = i.depth;
                return o;
            }
            ENDCG
        }
    }
}
