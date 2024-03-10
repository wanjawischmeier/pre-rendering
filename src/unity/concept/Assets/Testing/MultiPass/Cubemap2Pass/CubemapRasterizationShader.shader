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
            #define CUBEMAP_FACE_COUNT 6

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            uniform float MAX_DEPTH_DIFFERENCE, PROJ_DIFF;
            uniform float2 SCREEN_RESOLUTION, TARGET_TEXTURE_RESOLUTION;
            uniform float3 CUBE_POSITIONS[2];
            uniform float4x4 VP_I, ORIENTATION_MATRICIES[6];

            Texture2D<float4> _ApproximationTexture0, _ApproximationTexture1, _ApproximationTexture2, _ApproximationTexture3;
            Texture2DArray<float4> _CubemapTextures;
            SamplerState sampler_point_clamp, sampler_linear_clamp;

            // returns true if the corresponding triangles can be validated (out.x)
            // checks whether they can be uv-interpolated and               (out.y)
            // how the quad should be triangulated                          (out.z)
            bool2 ValidateQuad(float4 c_current, float4 c00, float4 c11, bool i00_eq_i11, float2 tc, int index)
            {
                bool2 o = true.xx;
                float2 off = float2(1, 0);
    
                if (i00_eq_i11)
                {
                    // triangulation order 0
        
                    // validate vertex 0: (0, 0)
                    if (c00.w == DEPTH_CLEAR_FLAG)
                    {
                        o.x = false;
                        return o;
                    }
                    if (c00.z != c_current.z)
                    {
                        o.y = false;
                    }
        
                    // validate vertex 1: (1, 1)
                    if (c11.w == DEPTH_CLEAR_FLAG)
                    {
                        o.x = false;
                        return o;
                    }
                    if (c11.z != c_current.z)
                    {
                        o.y = false;
                    }
        
                    if (index < 3)
                    {
                        // validate vertex 2: (1, 0)
                        float4 c10 = _ApproximationTexture0.SampleLevel(sampler_point_clamp, tc + off.xy, 0);
                        if (c10.w == DEPTH_CLEAR_FLAG)
                        {
                            o.x = false;
                            return o;
                        }
                        if (c10.z != c_current.z)
                        {
                            o.y = false;
                        }
                    }
                    else
                    {
                        // validate vertex 2: (0, 1)
                        float4 c01 = _ApproximationTexture0.SampleLevel(sampler_point_clamp, tc + off.yx, 0);
                        if (c01.w == DEPTH_CLEAR_FLAG)
                        {
                            o.x = false;
                            return o;
                        }
                        if (c01.z != c_current.z)
                        {
                            o.y = false;
                        }
                    }
                }
                else
                {
                    // triangulation order 1
        
                    // validate vertex 0: (0, 1)
                    float4 c01 = _ApproximationTexture0.SampleLevel(sampler_point_clamp, tc + off.yx, 0);
                    if (c01.w == DEPTH_CLEAR_FLAG)
                    {
                        o.x = false;
                        return o;
                    }
                    if (c01.z != c_current.z)
                    {
                        o.y = false;
                    }
        
                    // validate vertex 1: (1, 0)
                    float4 c10 = _ApproximationTexture0.SampleLevel(sampler_point_clamp, tc + off.xy, 0);
                    if (c10.w == DEPTH_CLEAR_FLAG)
                    {
                        o.x = false;
                        return o;
                    }
                    if (c10.z != c_current.z)
                    {
                        o.y = false;
                    }
        
                    if (index < 3)
                    {
                        // validate vertex 2: (0, 0)
                        if (c00.w == DEPTH_CLEAR_FLAG)
                        {
                            o.x = false;
                            return o;
                        }
                        if (c00.z != c_current.z)
                        {
                            o.y = false;
                        }
                    }
                    else
                    {
                        // validate vertex 2: (1, 1)
                        if (c11.w == DEPTH_CLEAR_FLAG)
                        {
                            o.x = false;
                            return o;
                        }
                        if (c11.z != c_current.z)
                        {
                            o.y = false;
                        }
                    }
                }
    
                return o;
            }

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
    
                if (index >= 3 && false)
                    return float2(0, 0);
    
                return tc;
            }

            float4 GetCubemapWorldSpacePosition(float3 cubemapUV, float depth)
            {
                int faceIndex = cubemapUV.z % CUBEMAP_FACE_COUNT;
                int cubemapIndex = (cubemapUV.z - faceIndex) / CUBEMAP_FACE_COUNT;
    
                depth = depth * (30 - 0.1) + 0.1;
                float2 viewSpace = (cubemapUV * 2 - 1) * depth;
                float4 pos = float4(viewSpace, depth, 1);
                pos = mul(ORIENTATION_MATRICIES[faceIndex], pos);
                float3 off0 = float3(1, -2, 0);
                // pos.xyz -= CAMERA_POS;
                // pos = mul(UNITY_MATRIX_VP, pos);
                // pos.xyz += CUBE_POSITIONS[cubemapIndex].xyz;
                pos.xyz -= CUBE_POSITIONS[cubemapIndex];
                return pos;
            }

            void ProjectQuad(inout v2f data)
{
    
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
    
                float4 c00 = _ApproximationTexture0.SampleLevel(sampler_point_clamp, uv_origin, 0);
                float4 c11 = _ApproximationTexture0.SampleLevel(sampler_point_clamp, uv_origin + texelSize, 0);
    
                bool i00_eq_i11 = c00.z == c11.z;
    
                float2 tc = TriangulateQuad(x, y, localIndex, i00_eq_i11);
                float2 uv = tc * texelSize;
    
                float4 c_current = _ApproximationTexture0.SampleLevel(sampler_point_clamp, uv, 0);
                if (c_current.w == DEPTH_CLEAR_FLAG)
                {
                    o.vertex = float4(0, 0, 0, 0);
                    return o;
                }
                /*
                bool2 quadValid = ValidateQuad(c_current, c00, c11, i00_eq_i11, tc, localIndex);
                if (!quadValid.x)
                {
                    // o.vertex = float4(0, 0, 0, 0);
                    // return o;
                }
                */
                float4 col = _CubemapTextures.SampleLevel(sampler_linear_clamp, c_current.xyz, 0);
                if (/* quadValid.y || */ true)
                {
                    o.uv = c_current;
                }
                else
                {
                    o.uv.xyz = col.rgb;
                    o.uv.w = -1;
                }
    
                // -- only a temporary check!
                // this is horrible for performance, never do this!!!
                float4 c10 = _ApproximationTexture0.SampleLevel(sampler_point_clamp, float2(uv_origin.x + texelSize.x, uv_origin.y), 0);
                float4 c01 = _ApproximationTexture0.SampleLevel(sampler_point_clamp, float2(uv_origin.x, uv_origin.y + texelSize.y), 0);
    
                float4 col00 = _CubemapTextures.SampleLevel(sampler_linear_clamp, c00.xyz, 0);
                float4 col10 = _CubemapTextures.SampleLevel(sampler_linear_clamp, c10.xyz, 0);
                float4 col01 = _CubemapTextures.SampleLevel(sampler_linear_clamp, c01.xyz, 0);
                float4 col11 = _CubemapTextures.SampleLevel(sampler_linear_clamp, c11.xyz, 0);
    
                if (abs(col.a - col00.a) > MAX_DEPTH_DIFFERENCE || abs(col.a - col10.a) > MAX_DEPTH_DIFFERENCE || abs(col.a - col01.a) > MAX_DEPTH_DIFFERENCE || abs(col.a - col11.a) > MAX_DEPTH_DIFFERENCE)
                {
                    o.vertex = float4(0, 0, 0, 0);
                    return o;
                }
    
                // -- end of this monstrosity
    
                // float depth = col.a * (30 - 0.1) + 0.1;
                // float2 viewSpace = (uv * 2 - 1) * depth;
                // viewSpace.x *= SCREEN_RESOLUTION.x / SCREEN_RESOLUTION.y;
                // float4 pos = float4(viewSpace.x + sin(depth) / 10 * 0, viewSpace.y + cos(depth) / 4 * 0, depth, 1);
                // o.uv = c00.wwww;
                // o.uv.w = -1;
                float4 pos = GetCubemapWorldSpacePosition(c_current.xyz, col.a);
                // float4 pos_pass2 = GetCubemapWorldSpacePosition(c_current_pass2.xyz, col_pass2.a);
                o.vertex = UnityObjectToClipPos(pos);
                /*
                float4 vertex_pass2 = UnityObjectToClipPos(pos_pass2);
                if (c_current_pass2.w != 0 && col_pass2.a < 0.9 && o.vertex.w - c_current.w > vertex_pass2.w - c_current.w + PROJ_DIFF)
                {
                    o.uv.xyz = col_pass2.rgb;
                    o.uv.w = -1;
                    o.vertex = vertex_pass2;
                    return o;
                }
                */
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col;
                
                if (i.uv.w == -1)
                {
                    col = fixed4(i.uv.rgb, 1);
                }
                else
                {
                    col = _CubemapTextures.Sample(sampler_linear_clamp, i.uv.xyz);
                }
    
                return col;
            }
            ENDCG
        }
    }
}
