Shader "Unlit/DistanceBasedSliceSelectionMeshShader"
{
    Properties
    {
        _Factor ("Factor", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define TRIANGULATION_VERTEX_RATIO 6
            #define DEPTH_CLEAR_FLAG 1

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 col : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _InitialPass;
            uniform float _Factor;
            uniform float2 TARGET_TEXTURE_RESOLUTION;

            float2 TriangulateQuad(float x, float y, int index, bool i00_eq_i11)
            {
                float2 tc;
    
                if (i00_eq_i11)
                {
                    // triangulation order 0
                    switch (index)
                    {
                        // tri 0
                        case 0: // (0, 0)
                            tc = float2(x, y);
                            break;
                        case 1: // (1, 1)
                            tc = float2(x + 1, y + 1);
                            break;
                        case 2: // (1, 0)
                            tc = float2(x + 1, y);
                            break;
        
                        // tri 1
                        case 3: // (1, 1)
                            tc = float2(x + 1, y + 1);
                            break;
                        case 4: // (0, 0)
                            tc = float2(x, y);
                            break;
                        case 5: // (0, 1)
                            tc = float2(x, y + 1);
                            break;
                    }
                }
                else
                {
                    // triangulation order 1
                    switch (index)
                    {
                        // tri 0
                        case 0: // (0, 1)
                            tc = float2(x, y + 1);
                            break;
                        case 1: // (1, 0)
                            tc = float2(x + 1, y);
                            break;
                        case 2: // (0, 0)
                            tc = float2(x, y);
                            break;
        
                        // tri 1
                        case 3: // (1, 0)
                            tc = float2(x + 1, y);
                            break;
                        case 4: // (0, 1)
                            tc = float2(x, y + 1);
                            break;
                        case 5: // (1, 1)
                            tc = float2(x + 1, y + 1);
                            break;
                    }
                }
    
                return tc;
            }

            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
        
                int localIndex = id % TRIANGULATION_VERTEX_RATIO;
                int vertexIndex = (id - localIndex) / TRIANGULATION_VERTEX_RATIO;
    
                float x = vertexIndex % TARGET_TEXTURE_RESOLUTION.x;
                float y = (vertexIndex - x) / TARGET_TEXTURE_RESOLUTION.x;
                float2 texelSize = 1 / TARGET_TEXTURE_RESOLUTION;
                float2 uv_origin = float2(x, y) * texelSize;
    
                o.col = tex2Dlod(_InitialPass, float4(uv_origin, 0, 0));
                if (o.col.w == DEPTH_CLEAR_FLAG)
                {
                    o.vertex = float4(0, 0, 0, 0);
                    return o;
                }
    
                float2 tc = TriangulateQuad(x, y, localIndex, true);
                float2 uv = tc * texelSize;
    
                float4 vertex = float4(uv.x, uv.y, o.col.a, 1);
                // float4 vertex = float4(uv.xy, o.col.z, 1);
                vertex.x += (o.col.b == 0 ? -1 : 1) * _Factor;
    
                o.vertex = UnityObjectToClipPos(vertex);
                o.uv = uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = tex2D(_InitialPass, i.uv);
                return i.col;
            }
            ENDCG
        }
    }
}
