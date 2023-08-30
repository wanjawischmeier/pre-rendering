Shader"PreRendering/HardwareAcceleratedMultiPass"
{
    SubShader
    {
        Pass
        {
            // Cull Off
            ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float depth : SV_Depth;
                int index : TEXCOORD1;
            };

            Texture2D<float4> _Input;

            StructuredBuffer<int> _Triangles;
            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float2> _UVs;

            float TIMESTEP, FCLIP;

            uniform uint _StartIndex;
            uniform uint _BaseVertexIndex;
            uniform float4x4 _ObjectToWorld;

            v2f vert(uint vertexID: SV_VertexID, uint instanceID : SV_InstanceID)
            {
                v2f o;
    
                int index = _Triangles[vertexID + _StartIndex] + _BaseVertexIndex;
                float2 uv0 = _UVs[index + 0];
                float2 uv1 = _UVs[index + 1];
                float2 uv2 = _UVs[index + 2];
                if (uv0.x == -1 || uv1.x == -1 || uv2.x == -1)
                {
                    o.pos = float4(0, 0, 0, 0);
                    return o;
                }
                
                float3 pos = _Positions[index];
                float4 wpos = mul(_ObjectToWorld, float4(pos + float3(instanceID, 0, 0), 1.0f));
                
                o.index = index;
                o.depth = length(wpos);
                // wpos.y += sin(TIMESTEP / 50) * sin(length(wpos)) * sin(wpos.x);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.uv = uv0;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                /*
                float2 uv0 = _UVs[_Triangles[i.index + 0] + _BaseVertexIndex];
                float2 uv1 = _UVs[_Triangles[i.index + 1] + _BaseVertexIndex];
                float2 uv2 = _UVs[_Triangles[i.index + 2] + _BaseVertexIndex];
                bool isEdge = length(i.uv - uv0) + length(i.uv - uv1) + length(i.uv - uv2) < 0.6;
                if (isEdge)
                {
                    return float4(0, 0, 0, 0);
                }
                */
                return float4(i.uv, i.depth, 1);
            }
            ENDCG
        }
    }
}
