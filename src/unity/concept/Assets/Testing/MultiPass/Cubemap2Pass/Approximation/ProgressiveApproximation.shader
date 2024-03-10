Shader "Unlit/ProgressiveApproximation"
{
    Properties
    {
        _PreviousApproximation ("Texture", 2D) = "white" {}
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

            #define CLIPPING_OFFSET 0.00001

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex_world : TEXCOORD0;
                float3 normal_world : TEXCOORD1;
                float3 screenPos : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            uniform float3 CUBE_POSITIONS[2];
            uniform float4x4 INVERSE_ORIENTATION_MATRICIES[6];
            sampler2D _PreviousApproximation;

            int GetCubemapFaceIndex(float3 vertex_world)
            {
                float3 vertex_world_abs = abs(vertex_world);
                float maxComponent = max(max(vertex_world_abs.x, vertex_world_abs.y), vertex_world_abs.z);
    
                if (maxComponent == vertex_world_abs.x)
                {
                    return vertex_world.x > 0 ? 0 : 1; // x: 0, -x: 1
                }
                else if (maxComponent == vertex_world_abs.y)
                {
                    return vertex_world.y > 0 ? 2 : 3; // y: 2, -y: 3
                }
                else
                {
                    return vertex_world.z > 0 ? 4 : 5; // z: 4, -z: 5
                }
            }

            float2 GetCubemapUV(float4 vertex_world, int faceIndex)
            {
                vertex_world = mul(INVERSE_ORIENTATION_MATRICIES[faceIndex], vertex_world);
                return (vertex_world.xy / vertex_world.z + 1) / 2;
            }

            v2f vert (appdata v)
            {
                v2f o;
    
                o.vertex_world = mul(unity_ObjectToWorld, v.vertex);
                o.normal_world = (UnityObjectToWorldNormal(v.normal)).xyz;
                o.vertex = UnityObjectToClipPos(v.vertex);
    
                // screen pos calculation inspired by: https://gamedev.stackexchange.com/a/129325
                o.screenPos = o.vertex.xyw;
                o.screenPos.y *= _ProjectionParams.x;
    
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 clipPos = i.screenPos.xy / i.screenPos.z;
                float2 screenUV = (clipPos + 1) / 2;

                float previousPassDepth = tex2D(_PreviousApproximation, screenUV).a;
                if (i.vertex.w <= previousPassDepth)
                {
                    discard;
                }
    
                int cubemapIndex = 0;
                float tmpAngle, lowestAngle = 0;
                float3 viewDir;
                for (int currentCubemapIndex = 0; currentCubemapIndex < 2; currentCubemapIndex++)
                {
                    float3 viewDir = i.vertex_world.xyz + CUBE_POSITIONS[currentCubemapIndex];
                    tmpAngle = asin(dot(i.normal_world, normalize(viewDir)));
        
                    if (tmpAngle < lowestAngle)
                    {
                        lowestAngle = tmpAngle;
                        cubemapIndex = currentCubemapIndex;
                    }
                }
    
                i.vertex_world.xyz += CUBE_POSITIONS[cubemapIndex];
                int cubemapFaceIndex = GetCubemapFaceIndex(i.vertex_world.xyz);
                float2 cubemapUV = GetCubemapUV(i.vertex_world, cubemapFaceIndex);
    
                return float4(cubemapUV, cubemapFaceIndex + cubemapIndex * 6, i.vertex.w);
            }
            ENDCG
        }
    }
}
