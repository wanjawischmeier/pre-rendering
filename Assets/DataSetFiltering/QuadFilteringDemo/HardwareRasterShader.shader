Shader "Unlit/HardwareRasterShader_Debug"
{
    Properties
    {
        _EdgeThreshold ("Triangle Edge Threshold", Range(0.01, 0.2)) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            uniform float _CameraNearClip, _CameraFarClip, _EdgeThreshold;
            uniform uint2 _InputResolution, _OutputResolution;

            Texture2D<int2> _VertexBuffer;
            StructuredBuffer<uint> _QuadIndexBuffer;
            
            bool IsVertexUsed(uint quadType, uint vtxOffsetIndex)
            {
                if (quadType == 3) return true; // both
                if (quadType == 1 && vtxOffsetIndex < 3) return true; // top only
                if (quadType == 2 && vtxOffsetIndex >= 3) return true; // bottom only

                return false;
            }

            // Maps index i <- [0,5] to the corresponding vertex offset and index in unit quad
            uint3 GetQuadVertexOffset(uint index)
            {
                static const uint3 offsets[6] = {
                    uint3(0, 0, 0),
                    uint3(1, 0, 1),
                    uint3(0, 1, 2),

                    uint3(0, 1, 2),
                    uint3(1, 0, 1),
                    uint3(1, 1, 3)
                };

                return offsets[index];
            }

            struct VSOutput
            {
                float4 pos : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            VSOutput vert(uint id : SV_VertexID)
            {
                VSOutput o;
                
                // Which quad does this vertex belong to?
                uint quadIndex = id / 6;
                uint vtxOffsetIndex = id % 6;
                uint packedIndex = _QuadIndexBuffer[quadIndex];
                uint quadType = packedIndex & 0x3;
                uint quadBaseIndex = packedIndex >> 2;

                if (!IsVertexUsed(quadType, vtxOffsetIndex))
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.bary = float3(0, 0, 0);
                    return o;
                }

                // Convert flat index back into 2D coordinates
                uint2 baseCoords;
                baseCoords.x = quadBaseIndex % _InputResolution.x;
                baseCoords.y = quadBaseIndex / _InputResolution.x;

                // Add vertex-specific offset (0–5) for this triangle vertex
                uint3 quadVertex = GetQuadVertexOffset(vtxOffsetIndex);
                uint2 sampleCoords = baseCoords + uint2(quadVertex.xy);

                // Use that to fetch final screen-space info from the lookup
                int2 vertLookup = _VertexBuffer[sampleCoords];
                uint2 vertCoords;
                vertCoords.x = vertLookup.x % _OutputResolution.x;
                vertCoords.y = vertLookup.x / _OutputResolution.x;

                float2 uv = float2(vertCoords) / _OutputResolution;
                float2 ndc = uv * 2.0 - 1.0;

                float depth = asfloat(vertLookup.y);

                o.pos = float4(ndc, depth, 1.0);
                o.bary = float3(quadVertex.z == 0 || quadVertex.z == 3, quadVertex.z == 1 || quadVertex.z == 3, quadVertex.z == 2);

                return o;
            }

            float4 frag(VSOutput i) : SV_Target
            {
                // return float4(0, 1, 0, 1);
                // return float4((i.bary.x / 10).xxx, 1);

                // Distance from edge (i.e., min bary value)
                float edge = min(min(i.bary.x, i.bary.y), i.bary.z);
                if (edge < _EdgeThreshold)
                {
                    return float4(1, 0, 0, 1); // Red edges
                }

                // Fill with bary UV for debugging
                return float4(i.bary.xy, 1.0 - i.bary.x - i.bary.y, 1);
            }
            ENDHLSL
        }
    }
}
