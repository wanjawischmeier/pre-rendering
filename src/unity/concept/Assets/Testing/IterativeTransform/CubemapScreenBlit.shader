Shader "Hidden/CubemapScreenBlit"
{
    Properties
    {
        _InputOutput ("Input Array", 2DArray) = "black" {}
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            Texture2DArray _InputOutput;
            SamplerState point_clamp_sampler; // TODO: implement bilinear cubemap sampling

            uniform float3 CUBE_POSITIONS[1];
            uniform float4x4 _ViewToWorldMatrix, _InvProjectionMatrix;
            uniform float4x4 INVERSE_ORIENTATION_MATRICIES[6];

            float4 WorldSpaceToCubemapUV(float3 worldPosition, int cubemapIndex)
            {
                float3 localPosition = worldPosition - CUBE_POSITIONS[cubemapIndex];

                // determine the face index based on the dominant axis
                int faceIndex;
                float3 absPos = abs(localPosition);
                float3 faceNormal;
    
                if (absPos.x >= absPos.y && absPos.x >= absPos.z)
                {
                    faceIndex = (localPosition.x > 0) ? 0 : 1; // +x or -x
                    faceNormal = float3(sign(localPosition.x), 0, 0);
                }
                else if (absPos.y >= absPos.x && absPos.y >= absPos.z)
                {
                    faceIndex = (localPosition.y > 0) ? 2 : 3; // +y or -y
                    faceNormal = float3(0, sign(localPosition.y), 0);
                }
                else
                {
                    faceIndex = (localPosition.z > 0) ? 4 : 5; // +z or -z
                    faceNormal = float3(0, 0, sign(localPosition.z));
                }

                // project local position onto the selected cubemap face
                float3 projected = localPosition - faceNormal * dot(localPosition, faceNormal);

                // calculate UV coordinates relative to the selected face
                float2 uv;
                if (faceIndex < 2)      // +x or -x
                    uv = float2(-projected.z, projected.y) / abs(localPosition.x);
                else if (faceIndex < 4) // +y or -y
                    uv = float2(projected.x, -projected.z) / abs(localPosition.y);
                else                    // +z or -z
                    uv = float2(projected.x, projected.y) / abs(localPosition.z);

                // transform UV to [0, 1] range
                uv = uv * 0.5 + 0.5;

                if (faceIndex == 1) uv.x = 1.0 - uv.x; // -x: Flip horizontally
                if (faceIndex == 3) uv.y = 1.0 - uv.y; // -y: Flip vertically
                if (faceIndex == 5) uv.x = 1.0 - uv.x; // -z: flip horizontally

                float depth = length(localPosition);
                return float4(uv, depth, faceIndex);
            }


            fixed4 frag (v2f i) : SV_Target
            {
                // convert screen-space UV to NDC [-1, 1]
                float2 ndc = i.uv * 2 - 1;
                float4 clipPos = float4(ndc.xy, 1, 1);
                
                float4 viewPos = mul(_InvProjectionMatrix, clipPos);
                viewPos /= viewPos.w; // homogeneous divide

                // convert to world-space direction
                float3 worldDir = mul((float3x3)_ViewToWorldMatrix, viewPos.xyz);
                float4 cubemapUV = WorldSpaceToCubemapUV(worldDir, 0);

                float4 color = _InputOutput.Sample(point_clamp_sampler, cubemapUV.xyw);
                float4 realtime = tex2D(_MainTex, i.uv);
                return color;
                // return realtime.a == 0 ? color : realtime;
            }
            ENDCG
        }
    }
}
