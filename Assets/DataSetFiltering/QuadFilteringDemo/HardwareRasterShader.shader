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

            StructuredBuffer<float3x3> _Vertices;

            struct VSOutput
            {
                float4 pos : SV_POSITION;
                float3 bary : TEXCOORD0;
            };

            VSOutput vert(uint id : SV_VertexID)
            {
                VSOutput o;

                // Assume triangles are laid out consecutively
                uint vtxIndex = id % 3;
                uint triIndex = (id - vtxIndex) / 3;
                float3x3 verts = _Vertices[triIndex];

                o.pos = float4(verts[vtxIndex], 1);
                o.pos.y = -o.pos.y; // Flip Y for NDC
                o.pos.z = 0;

                o.bary = float3(vtxIndex == 0, vtxIndex == 1, vtxIndex == 2);

                return o;
            }

            float4 frag(VSOutput i) : SV_Target
            {
                // return float4(0, 1, 0, 1);
                // Wireframe threshold
                float edgeThreshold = 0.01;

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
