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

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            StructuredBuffer<int> _Triangles;
            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float2> _UVs;
            uniform uint _StartIndex;
            uniform uint _BaseVertexIndex;
            uniform float4x4 _ObjectToWorld;
            uniform float _NumInstances;

            v2f vert(uint vertexID: SV_VertexID, uint instanceID : SV_InstanceID)
            {
                v2f o;
                float2 uv = _UVs[_Triangles[vertexID + _StartIndex] + _BaseVertexIndex];
                float3 pos = _Positions[_Triangles[vertexID + _StartIndex] + _BaseVertexIndex];
                float4 wpos = mul(_ObjectToWorld, float4(pos + float3(instanceID, 0, 0), 1.0f));
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.uv = uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                return fixed4(i.uv, 0, 1);
            }
            ENDCG
        }
    }
}
