Shader"PreRendering/ProjectionSurface"
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
            uniform float4x4 _ObjectToWorld;

            v2f vert(appdata v)
            {
                v2f o;
    
                int index = _Triangles[v.vertexID];
                float2 uv0 = _UVs[index + 0];
                float2 uv1 = _UVs[index + 1];
                float2 uv2 = _UVs[index + 2];
                if (uv0.x == -1 || uv1.x == -1 || uv2.x == -1)
                {
                    o.pos = float4(0, 0, 0, 0);
                    o.uv = float2(0, 0);
                    o.depth = 0;
                    return o;
                }
                
                float3 pos = _Positions[index];
                float4 wpos = mul(_ObjectToWorld, float4(pos, 1.0f));
    
                // wpos.y += sin(TIMESTEP * 2) * sin(wpos.x);
    
                o.depth = length(wpos);
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.uv = uv0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return float4(i.uv, i.depth, TEXTURE_INDEX + 1);
            }
            ENDCG
        }
    }
}
