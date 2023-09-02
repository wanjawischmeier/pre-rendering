Shader"PreRendering/PostRasterization"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Off

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

            int TEXTURE_INDEX;
            float2 RESOLUTION;
            sampler2D _MainTex;
            sampler2D _Input0, _Input1;
            sampler2D _Coordinates0, _Coordinates1;
            
            // propably violating the genova convention
            // the camera refused to render onto multiple slices, so this has to exist :/
            #define SAMPLE_PSEUDO_ARRAY(array, uv, slice, result)   \
                switch (slice) {                                    \
                    case 0:                                         \
                        result = tex2D(array##0, uv);               \
                        break;                                      \
                    case 1:                                         \
                        result = tex2D(array##1, uv);               \
                        break;                                      \
                    default:                                        \
                        result = float4(1, 0, 1, 1);                \
                        break;                                      \
                }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 tc;
                SAMPLE_PSEUDO_ARRAY(_Coordinates, i.uv, TEXTURE_INDEX, tc);
                float4 col = tex2D(_MainTex, i.uv);
                
                if (tc.a == 0) // col.a == 1 with clear flags as solid color
                {
                    return col;
                }
                
                float2 uv = tc.xy;
                SAMPLE_PSEUDO_ARRAY(_Input, uv, TEXTURE_INDEX, col);
                return col;
            }
            ENDCG
        }
    }
}
