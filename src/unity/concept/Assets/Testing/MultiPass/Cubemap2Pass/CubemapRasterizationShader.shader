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
                float4 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float2 SCREEN_RESOLUTION, TARGET_TEXTURE_RESOLUTION;
            float4x4 VP_I;

            Texture2D<float4> _ApproximationTargetTexture;
            Texture2DArray<float4> _CubemapTextures;
            SamplerState sampler_point_clamp, sampler_linear_clamp;

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
                float4 coords = _ApproximationTargetTexture.SampleLevel(sampler_point_clamp, uv.xy, 0);
                o.uv = float4(coords.xyz, 0);
                float index = coords.z;
                float depth = coords.w;
    
                float2 index_depth = _ApproximationTargetTexture.SampleLevel(sampler_point_clamp, uv00.xy, 0).zw;
                if (index_depth.x != index) { index = -1; }
                if (index_depth.y == DEPTH_CLEAR_FLAG)
                {
                    o.vertex = float4(0, 0, 0, 1);
                    return o;
                }
                else
                {
                    float4 uv11 = uv00 + texelSize.xyzz;
                    index_depth = _ApproximationTargetTexture.SampleLevel(sampler_point_clamp, uv11.xy, 0).zw;
        
                    if (index_depth.x != index) { index = -1; }
                    if (index_depth.y == DEPTH_CLEAR_FLAG)
                    {
                        o.vertex = float4(0, 0, 0, 1);
                        return o;
                    }
                    else if (localIndex < 3)
                    {
                        float4 uv10 = uv00 + texelSize.xzzz;
                        index_depth = _ApproximationTargetTexture.SampleLevel(sampler_point_clamp, uv10.xy, 0).zw;
            
                        if (index_depth.x != index) { index = -1; }
                        if (index_depth.y == DEPTH_CLEAR_FLAG)
                        {
                            o.vertex = float4(0, 0, 0, 1);
                            return o;
                        }
                    }
                    else
                    {
                        float4 uv01 = uv00 + texelSize.zyzz;
                        index_depth = _ApproximationTargetTexture.SampleLevel(sampler_point_clamp, uv01.xy, 0).zw;
            
                        if (index_depth.x != index) { index = -1; }
                        if (index_depth.y == DEPTH_CLEAR_FLAG)
                        {
                            o.vertex = float4(0, 0, 0, 1);
                            return o;
                        }
                    }
                }
    
                
                if (depth == DEPTH_CLEAR_FLAG)
                {
                    o.vertex = float4(0, 0, 0, 1);
                    return o;
                }
                else if (index == -1)
                {
                    o.uv.xyz = _CubemapTextures.SampleLevel(sampler_linear_clamp, coords.xyz, 0).rgb;
                    o.uv.w = 1;
                }
    
                float2 viewSpace = (uv * 2 - 1) * depth;
                viewSpace.x *= SCREEN_RESOLUTION.x / SCREEN_RESOLUTION.y;
                float4 pos = float4(viewSpace.x + sin(depth) / 10 * 0, viewSpace.y + cos(depth) / 4 * 0, depth, 1);
                
                o.vertex = UnityObjectToClipPos(pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col;
                
                if (i.uv.w == 0)
                {
                    col = _CubemapTextures.Sample(sampler_linear_clamp, i.uv.xyz);
                }
                else
                {
                    col = fixed4(i.uv.rgb, 1);
                }
    
                return col;
            }
            ENDCG
        }
    }
}
