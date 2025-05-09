Shader "Unlit/HardwareRasterShader_Debug"
{
    Properties {}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            StructuredBuffer<float4> _Vertices;

            struct VSOutput
            {
                float4 pos : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            VSOutput vert(uint id : SV_VertexID)
            {
                VSOutput o;

                // Assume triangles are laid out consecutively
                uint triIndex = id / 3;
                uint vtxIndex = triIndex * 3;

                float4 v0 = _Vertices[vtxIndex + 0];
                float4 v1 = _Vertices[vtxIndex + 1];
                float4 v2 = _Vertices[vtxIndex + 2];

                uint corner = id % 3;
                if (corner == 0) {
                    o.pos = v0;
                    o.bary = float3(1, 0, 0);
                }
                else if (corner == 1) {
                    o.pos = v1;
                    o.bary = float3(0, 1, 0);
                }
                else {
                    o.pos = v2;
                    o.bary = float3(0, 0, 1);
                }

                return o;
            }

            float4 frag(VSOutput i) : SV_Target
            {
                // Wireframe threshold
                float edgeThreshold = 0.03;

                // Distance from edge (i.e., min bary value)
                float edge = min(min(i.bary.x, i.bary.y), i.bary.z);

                if (edge < edgeThreshold)
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
