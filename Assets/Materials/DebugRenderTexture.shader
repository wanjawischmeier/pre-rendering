Shader "Unlit/DebugRenderTexture"
{
    Properties
    {
        _SliceOffset ("Slice Offset", Integer) = 0
        _ValueFactor ("Value Factor", Range(0, 1)) = 0.5
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            Texture2DArray _Input;
            float4 _Input_ST;
            SamplerState linear_clamp_sampler;

            uniform int _SliceOffset;
            uniform float _ValueFactor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Input);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Determine grid layout (2 columns, 3 rows)
                float2 uv = i.uv;
                int faceIndex = floor(uv.x * 2) + floor(uv.y * 3) * 2; 
                faceIndex = (faceIndex + (int)_SliceOffset) % 6; // Offset by _SliceOffset
    
                // Remap UV to fit within each cell
                uv.x = frac(uv.x * 2);
                uv.y = frac(uv.y * 3);
    
                float3 tc = float3(uv, faceIndex);
                float value = asfloat(_Input.Sample(linear_clamp_sampler, tc));
    
                // return float4(uv, 0, 1);
                return value.xxxx * _ValueFactor;
            }
            ENDCG
        }
    }
}
