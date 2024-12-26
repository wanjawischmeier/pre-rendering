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
            Texture2DArray _InputOutput, _Downsampled;
            SamplerState point_clamp_sampler; // TODO: implement bilinear cubemap sampling

            uniform float3 CUBE_POSITIONS[1];
            uniform float4x4 _ViewToWorldMatrix, _InvProjectionMatrix;
            uniform float4x4 INVERSE_ORIENTATION_MATRICIES[6];

            #include "IterativeTransformUtility.cginc"

            fixed4 frag (v2f i) : SV_Target
            {
                // convert screen-space UV to NDC [-1, 1]
                float2 ndc = i.uv * 2 - 1;
                float4 clipPos = float4(ndc.xy, 1, 1);
                
                float4 viewPos = mul(_InvProjectionMatrix, clipPos);
                viewPos /= viewPos.w; // homogeneous divide

                // convert to world-space direction
                float3 worldPosition = mul((float3x3)_ViewToWorldMatrix, viewPos.xyz);
                float3 localPosition = worldPosition + CUBE_POSITIONS[0];
                float4 cubemapUV = WorldSpaceToCubemapUV(localPosition, 0);

                float4 color = _InputOutput.Sample(point_clamp_sampler, cubemapUV.xyw);
                if (color.a == 0)
                {
                    color = _Downsampled.Sample(point_clamp_sampler, cubemapUV.xyw);
                }
                // float4 realtime = tex2D(_MainTex, i.uv);
                return color;
                // return realtime.a == 0 ? color : realtime; // TODO: implement depth sorting
            }
            ENDCG
        }
    }
}
