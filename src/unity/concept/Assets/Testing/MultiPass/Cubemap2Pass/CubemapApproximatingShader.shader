Shader "Unlit/CubemapApproximatingShader"
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
            Texture2DArray<float4> _CubemapTextures;
            SamplerState sampler_linear_repeat;
            uniform float4x4 INVERSE_ORIENTATION_MATRICIES[6];

            int GetCubemapFaceIndex(float3 pos)
            {
                float3 absPos = abs(pos);
                float maxComponent = max(max(absPos.x, absPos.y), absPos.z);
    
                if (maxComponent == absPos.x)
                {
                    return pos.x < 0 ? 0 : 1; // x: 0, -x: 1
                }
                else if (maxComponent == absPos.y)
                {
                    return pos.y < 0 ? 2 : 3; // y: 2, -y: 3
                }
                else
                {
                    return pos.z < 0 ? 4 : 5; // z: 4, -z: 5
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
                pos.xyz += float3(9, 0, 2);
    
                int faceIndex = GetCubemapFaceIndex(pos.xyz);
    
                pos = mul(INVERSE_ORIENTATION_MATRICIES[TEXTURE_INDEX], pos);
                float2 viewSpace = (pos.xy / pos.z + 1) / 2;
                
                fixed4 col = fixed4(viewSpace, 0, 1);
                if (!(viewSpace.x < 0 || viewSpace.x > 1 || viewSpace.y < 0 || viewSpace.y > 1 || pos.z < 0))
                {
                    col = _CubemapTextures.Sample(sampler_linear_repeat, float3(viewSpace, 4));
                }
                return col;
            }
            ENDCG
        }
    }
}
