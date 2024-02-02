Shader"PreRendering/PostRasterization"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // no culling or depth
        Cull Off ZWrite Off ZTest Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Macros.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 projPos : TEXCOORD0;
                float3 camRelativeWorldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.projPos = ComputeScreenPos(o.vertex);
                o.camRelativeWorldPos = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz - _WorldSpaceCameraPos;
                o.uv = v.uv;
                return o;
            }
            
            #define FCLIP 30
            #define NCLIP 0.1

            uniform int NUM_SLICES, DEBUG_MODE, UI_DEBUGGER, SLICE, MAX_CIRCUMFERENCE;
            uniform float INTERPOLATION_RANGE, DEPTH_OFFSET;
            uniform float2 RESOLUTION;
            uniform float4 CAMERA_POSITION;
            uniform float4 CUBE_POSITIONS[3];
            uniform float4x4 VP_I;
            uniform float4x4 ORIENTATION_MATRICIES[6];
            uniform float4x4 INVERSE_ORIENTATION_MATRICIES[6];
            
            SamplerState sampler_linear_repeat;
            Texture2D _MainTex, _CameraTex, _UI;
            Texture2D _Input0, _Input1, _Input2, _Input3, _Input4, _Input5, _Input6, _Input7;
            Texture2D _Coordinates0, _Coordinates1, _Coordinates2, _Coordinates3, _Coordinates4, _Coordinates5, _Coordinates6, _Coordinates7;
            Texture2D _Depth0, _Depth1, _Depth2, _Depth3, _Depth4, _Depth5, _Depth6, _Depth7;
            Texture2DArray<float4> _CubemapFaces;

            float4 interpolateColors(float4 color0, float4 color1, float blurriness0, float blurriness1)
            {
                float deltaBlurriness = blurriness0 - blurriness1;
                if (abs(deltaBlurriness) <= INTERPOLATION_RANGE)
                {
                    // interpolate between color0 and color1 based on the difference in blurriness values
                    float t = saturate(deltaBlurriness / INTERPOLATION_RANGE);
                    // return t.xxxx;
                    return lerp(color0, color1, t);
                }
                else
                {
                    // if the difference is outside the range, select the color with the least blurryness
                    return (blurriness0 < blurriness1) ? color0 : color1;
                }
            }

            void sampleLeastBlurrySlices(float2 uv, out float4 slice0, out float4 slice1, out float d)
            {
                float4 tc;
                float d0, d1 = 0;
    
                // initialize slices with high invalid blurryness
                slice0 = float4(0, 0, MAX_CIRCUMFERENCE, 0);
                slice1 = float4(0, 0, MAX_CIRCUMFERENCE, 0);

                for (int slice = 0; slice < MAX_SLICES - 1; slice++)
                {
                    SAMPLE_PSEUDO_ARRAY(_Coordinates, uv, slice, tc);
                    
                    // check if the slice is valid
                    if (tc.w >= 1)
                    {
                        SAMPLE_PSEUDO_ARRAY(_Depth, uv, slice, d0);
                        
                        // sort using depth layers
                        if (d0 > d1 + DEPTH_TOLERANCE)
                        {
                            // new closest layer, clear least blurry slices
                            slice0 = tc;
                            slice1 = float4(0, 0, MAX_CIRCUMFERENCE, 0);
                            d1 = d0;
                            d = d0;
                        }
                        else if (d0 > d1 - DEPTH_TOLERANCE)
                        {
                            // pixel is in current layer, compare blurryness values
                            if (tc.z < slice0.z)
                            {
                                slice1 = slice0;
                                slice0 = tc;
                            }
                            else if (tc.z < slice1.z)
                            {
                                slice1 = tc;
                            }
                        }
                    }
                }
            }

            float3 getViewDirection(v2f i)
            {
                float4 clipPos = float4(i.uv * 2.0 - 1.0, 1.0, 1.0);

                // Apply the inverse projection matrix to get the camera space position
                float4 camPos = mul(VP_I, clipPos);

                // Divide by the w component to get the direction vector in camera space
                return camPos.xyz / camPos.w;

                // Transform the direction from camera space to world space
                // return mul((float3x3)unity_WorldToCamera, direction);
            }

            // point to the view ray starting at the coordinate origin
            // https://mathworld.wolfram.com/Point-LineDistance3-Dimensional.html
            // x0: p, x1: (0, 0, 0), x2: d
            float getDistancePointToViewRay(float3 p, float3 d)
            {
                float numerator = abs(cross(p, p - d));
                return numerator / length(d);
            }

            float3 IntersectViewRayWithCubemaps(float3 viewDirection, out int faceIndex, out int cubemapIndex)
            {
                int currentCubemapIndex, closestFaceIndex, closestCubemapIndex;
                float t, closestFaceDistance = MAX_INT_CONST;
                float closestCubeDistance = closestFaceDistance;
                float3 intersectionPoint, closestFaceIntersection, closestCubeIntersection = float3(0, 0, 0);
                float3 cameraPositionFaceSpace, viewDirectionFaceSpace;
                float4 cubePosition;
                float4x4 invOrientationMatrix;
    
                faceIndex = -1;
                cubemapIndex = -1;
        
                [unroll]
                for (int currentFaceIndex = 0; currentFaceIndex < CUBEMAP_FACE_COUNT; currentFaceIndex++)
                {
                    // transform the view ray to face space
                    invOrientationMatrix = INVERSE_ORIENTATION_MATRICIES[currentFaceIndex];
                    viewDirectionFaceSpace = mul(invOrientationMatrix, float4(viewDirection, 1)).xyz;
        
                    // avoid potential precision issues by checking if the ray is close to parallel to the face
                    if (viewDirectionFaceSpace.z > 0.0001)
                    {
                        [unroll]
                        for (currentCubemapIndex = 0; currentCubemapIndex < 2; currentCubemapIndex++)
                        {
                            // transform the camera position to face space
                            cubePosition = CUBE_POSITIONS[currentCubemapIndex];
                            cameraPositionFaceSpace = mul(invOrientationMatrix, CAMERA_POSITION - cubePosition).xyz;
            
                            // shift cube faces one unit away from origin (along face normal)
                            cameraPositionFaceSpace.z -= CUBEMAP_SCALE;
                
                            // intersect the current face of all cubes with the view ray
                            t = -cameraPositionFaceSpace.z / viewDirectionFaceSpace.z;
                            intersectionPoint = cameraPositionFaceSpace + t * viewDirectionFaceSpace;

                            // get the closest intersecting point
                            if (all(abs(intersectionPoint) <= CUBEMAP_SCALE) && t > 0 && t < closestFaceDistance)
                            {
                                closestFaceDistance = t;
                                closestFaceIntersection = intersectionPoint;
                                closestCubemapIndex = currentCubemapIndex;
                            }
                        }
    
                        if (closestFaceDistance < closestCubeDistance)
                        {
                            // the initial or a closer face intersects with the view ray
                            closestCubeDistance = closestFaceDistance;
                            closestCubeIntersection = closestFaceIntersection;
                            faceIndex = currentFaceIndex;
                            cubemapIndex = closestCubemapIndex;
                        }
                    }
                }
    
                return closestCubeIntersection;
            }

            float3 ScreenUVToCubemapUV(float2 screenUV)
            {
                float4 viewRayClip = float4(screenUV * 2 - 1, 0, 1);
                float3 viewRayWorld = mul(VP_I, viewRayClip).xyz;
                
                int faceIndex, cubemapIndex;
                float3 intersectionPoint = IntersectViewRayWithCubemaps(viewRayWorld, faceIndex, cubemapIndex);
                if (faceIndex == -1)
                {
                    return float3(-1, -1, -1);
                }
                
                return float3(intersectionPoint.xy / CUBEMAP_SCALE / 2 + 0.5, CUBEMAP_FACE_COUNT * cubemapIndex + faceIndex);
            }

            float3 GetCubemapWorldSpacePosition(float3 cubemapUV)
            {
                int faceIndex = cubemapUV.z % CUBEMAP_FACE_COUNT;
                int cubemapIndex = (cubemapUV.z - faceIndex) / CUBEMAP_FACE_COUNT;
                float depth = _CubemapFaces.Sample(sampler_linear_repeat, cubemapUV).a;
                depth = depth * (FCLIP - NCLIP) + NCLIP;
    
                float2 viewSpace = (cubemapUV * 2 - 1) * depth;
                float4 pos = float4(viewSpace, depth, 1);
                pos = mul(ORIENTATION_MATRICIES[faceIndex], pos);
                return pos + 0 * CUBE_POSITIONS[cubemapIndex];
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                /*
                float depth = 1;
                float2 viewSpace = float2(2 * i.uv.x - 1, 1 - 2 * i.uv.y) * depth;
                float4 pos = float4(viewSpace, depth, 1);
                // pos = float4(1, 2, 3, 4);
                float4 pos2 = mul(ORIENTATION_MATRICIES[0], pos);
                // return fixed4(all(pos2 == float4(-1, 2, -3, 4)).xxx, 1);
                // return pos2;
                */
                /*
                float3 cubemapUV = ScreenUVToCubemapUV(i.uv);
                if (cubemapUV.z == -1)
                {
                    return fixed4(0, 0, 0, 1);
                }
                
                float3 pos = GetCubemapWorldSpacePosition(cubemapUV);
                */
                // return fixed4(pos, 1);
                // return fixed4(cubemapUV.xy, cubemapUV.z / 5.0, 1);
                // return _CubemapFaces.Sample(sampler_linear_repeat, cubemapUV).aaaa;
                /*
                float4 uv = _Coordinates0.Sample(sampler_linear_repeat, i.uv);
                if (uv.w >= 1)
                {
                    float4 col2;
                    SAMPLE_PSEUDO_ARRAY(_Input, uv.xy, uv.w - 1, col2);
                    return col2;
                }
                else
                {
                    return _MainTex.Sample(sampler_linear_repeat, i.uv);
                }
                */
                float4 clipPos = float4(i.uv * 2.0 - 1.0, 1.0, 1.0);
                float4 worldPos = mul(VP_I, clipPos);
                float3 P = worldPos.xyz / worldPos.w;
                // direction = ComputeWorldSpacePosition(i.uv, 1, UNITY_MATRIX_I_VP);
                // float distance = getDistancePointToViewRay(float3(1, 1, 1), direction);
    
                // return fixed4(P, 1);
    
                fixed4 ui;
                if (UI_DEBUGGER == 1)
                {
                    ui = _UI.Sample(sampler_linear_repeat, i.uv);
                    if (any(ui) && ui.g != ui.b)
                    {
                        return ui;
                    }
                }
    
                float4 col = _MainTex.Sample(sampler_linear_repeat, i.uv);
                if (col.a == 0 && DEBUG_MODE != 4 && DEBUG_MODE != 5)
                {
                    col = _CameraTex.Sample(sampler_linear_repeat, i.uv);
                }
    
                return MIX_COL_UI_BY_ALPHA(col, ui);
            }
            ENDCG
        }
    }
}
