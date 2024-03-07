Shader "Unlit/CubemapApproximationShader"
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 pos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            int TEXTURE_INDEX;
            float3 OFF;
            uniform float4x4 INVERSE_ORIENTATION_MATRICIES[6];

            int GetCubemapFaceIndex(float3 pos)
            {
                float3 absPos = abs(pos);
                float maxComponent = max(max(absPos.x, absPos.y), absPos.z);
    
                if (maxComponent == absPos.x)
                {
                    return pos.x > 0 ? 0 : 1; // x: 0, -x: 1
                }
                else if (maxComponent == absPos.y)
                {
                    return pos.y > 0 ? 2 : 3; // y: 2, -y: 3
                }
                else
                {
                    return pos.z > 0 ? 4 : 5; // z: 4, -z: 5
                }
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pos = v.vertex.xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 pos = float4(i.pos, 1);
                pos = mul(unity_ObjectToWorld, pos);
    
                float3 off0 = float3(1, -2, 0);
                float3 off1 = float3(1, -2, -4);
                
                int indexOff = 0;
                if (length((pos.xyz + off0) - _WorldSpaceCameraPos) < length((pos.xyz + off1) - _WorldSpaceCameraPos) || true)
                {
                    pos.xyz += off0;
                }
                else
                {
                    pos.xyz += off1;
                    indexOff = 6;
                }
    
                int faceIndex = GetCubemapFaceIndex(pos.xyz);
    
                pos = mul(INVERSE_ORIENTATION_MATRICIES[faceIndex], pos);
                float2 viewSpace = (pos.xy / pos.z + 1) / 2;
    
                float depth = length(i.pos + _WorldSpaceCameraPos);
                return float4(viewSpace, faceIndex + indexOff, depth);
            }
            ENDCG
        }
    }
}
