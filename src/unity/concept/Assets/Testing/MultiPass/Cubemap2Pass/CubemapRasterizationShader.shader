Shader "Unlit/CubemapRasterizationShader"
{
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
            #define DEPTH_CLEAR_FLAG 0

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float2 SCREEN_RESOLUTION, TARGET_TEXTURE_RESOLUTION;
            float4x4 VP_I;

            Texture2D<float4> _ApproximationTargetTexture;
            Texture2DArray<float4> _CubemapTextures;
            SamplerState sampler_linear_repeat;

            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                
                int localIndex = id % TRIANGULATION_VERTEX_RATIO;
                int vertexIndex = (id - localIndex) / TRIANGULATION_VERTEX_RATIO;
    
                float x = vertexIndex % TARGET_TEXTURE_RESOLUTION.x;
                float y = (vertexIndex - x) / TARGET_TEXTURE_RESOLUTION.x;
                float4 tc;
    
                switch (localIndex)
                {
                    case 0:                 // (0, 0)
                        tc = float4(x, y, 0, 0);
                        break;
                    case 1:                 // (1, 1)
                        tc = float4(x + 1, y + 1, 0, 0);
                        break;
                    case 2:                 // (1, 0)
                        tc = float4(x + 1, y, 0, 0);
                        break;
                    case 3:                 // (1, 1)
                        tc = float4(x + 1, y + 1, 0, 0);
                        break;
                    case 4:                 // (0, 0)
                        tc = float4(x, y, 0, 0);
                        break;
                    case 5:                 // (0, 1)
                        tc = float4(x, y + 1, 0, 0);
                        break;
                }
                
                float3 texelSize = float3(1 / TARGET_TEXTURE_RESOLUTION, 0);
                float4 uv = tc * texelSize.xyzz;
                
                float4 uv00 = float4(x, y, 0, 0) * texelSize.xyzz;
                float4 coords = _ApproximationTargetTexture.SampleLevel(sampler_linear_repeat, uv00.xy, 0);
                float depth = coords.a;
                
                float4 vertex = float4(tc.x / 20, tc.y / 20 - 6, depth, 1);
                
                if (depth == DEPTH_CLEAR_FLAG)
                {
                    vertex.xyz = 0;
                }
                else
                {
                    float4 uv11 = uv00 + texelSize.xyzz;
                    float tmp_depth = _ApproximationTargetTexture.SampleLevel(sampler_linear_repeat, uv11.xy, 0).a;
        
                    if (tmp_depth == DEPTH_CLEAR_FLAG)
                    {
                        vertex.xyz = 0;
                    }
                    else if (localIndex < 3)
                    {
                        float4 uv10 = uv00 + texelSize.xzzz;
                        tmp_depth = _ApproximationTargetTexture.SampleLevel(sampler_linear_repeat, uv10.xy, 0).a;
            
                        if (tmp_depth == DEPTH_CLEAR_FLAG)
                        {
                            vertex.xyz = 0;
                        }
                    }
                    else
                    {
                        float4 uv01 = uv00 + texelSize.zyzz;
                        tmp_depth = _ApproximationTargetTexture.SampleLevel(sampler_linear_repeat, uv01.xy, 0).a;
            
                        if (tmp_depth == DEPTH_CLEAR_FLAG)
                        {
                            vertex.xyz = 0;
                        }
                    }
                }
                
                // col = fixed4(col.aaa, 1);
                
                /*
                if (id == 0)
                {
                    vertex.xyz = float3(0, 0, 0);
                }
                else if (id == 1)
                {
                    vertex.xyz = float3(0, 1, 0);
                }
                else if (id == 2)
                {
                    vertex.xyz = float3(1, 1, 0);
                }
                else
                {
                    vertex.xyz = float3(0, 0, 0);
                }
                */
                // v.vertex.z += 6;
    
                float2 viewSpace = (uv * 2 - 1) * depth;
                viewSpace.x *= SCREEN_RESOLUTION.x / SCREEN_RESOLUTION.y;
                // viewSpace.y *= -1;
                float4 pos = float4(viewSpace.x + sin(depth) / 10 * 0, viewSpace.y + cos(depth) / 4 * 0, depth, 1);
                // pos.yz += float3(-24, -7.5, 22).yz;
                // vertex = mul(VP_I, pos);
                // vertex.xyz = ComputeWorldSpacePosition(viewSpace, depth, VP_I);
                
                o.vertex = UnityObjectToClipPos(pos);
                // o.vertex = float4(uv00.xy, 1, 1);
                // o.vertex.xy = uv00.yx;
                o.uv = coords.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // float4 col = _ApproximationTargetTexture.SampleLevel(sampler_linear_repeat, i.uv.xy, 0);
                fixed4 col = _CubemapTextures.Sample(sampler_linear_repeat, i.uv);
                // col = fixed4(col.xyz, 1);
                // fixed4 col = fixed4(i.render ? i.uv : float2(0, 0), 0, i.render ? 1 : 0);
                // fixed4 col = fixed4(i.uv, 0, 1);
                return col;
            }
            ENDCG
        }
    }
}
