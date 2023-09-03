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

            StructuredBuffer<int> _Triangles;
            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float2> _UVs;
            
            uniform int DEBUG_MODE, RENDER_PASS, TEXTURE_INDEX;
            uniform float TIMESTEP, MAX_CIRCUMFERENCE;
            uniform uint _StartIndex;
            uniform uint _BaseVertexIndex;
            uniform float4x4 _ObjectToWorld;

            v2f vert(appdata v)
            {
                v2f o;
    
                int index = _Triangles[v.vertexID + _StartIndex] + _BaseVertexIndex;
                float2 uv0 = _UVs[index + 0];
                float2 uv1 = _UVs[index + 1];
                float2 uv2 = _UVs[index + 2];
                if (uv0.x == -1 || uv1.x == -1 || uv2.x == -1)
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.uv = float2(0, 0);
                    return o;
                }
                
                float3 pos0 = _Positions[index + 0];
                float3 pos1 = _Positions[index + 1];
                float3 pos2 = _Positions[index + 2];
                float l0 = length(pos0 - pos1);
                float l1 = length(pos1 - pos2);
                float l2 = length(pos2 - pos0);
                o.perimeter = l0 + l1 + l2;
    
                if (RENDER_PASS != 0 && (l0 > MAX_CIRCUMFERENCE || l1 > MAX_CIRCUMFERENCE || l2 > MAX_CIRCUMFERENCE))
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.uv = float2(0, 0);
                    return o;
                }
    
                float4 wpos = mul(_ObjectToWorld, float4(pos0, 1.0f));
    
                if (DEBUG_MODE == 1 && length(wpos.xyz - float3(-4, 0, -5)) < 10) // zSineFilled
                {
                    wpos.y += sin(TIMESTEP * 2) * sin(length(wpos)) * sin(wpos.x);
                }
    
                o.depth = length(wpos);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.uv = uv0;
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
