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
            
            #define MAX_SLICES 4

            uniform int SLICES;
            uniform float2 RESOLUTION;

            sampler2D _MainTex;
            sampler2D _Input0, _Input1, _Input2, _Input3;
            sampler2D _Coordinates0, _Coordinates1, _Coordinates2, _Coordinates3;
            
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
                    case 2:                                         \
                        result = tex2D(array##2, uv);               \
                        break;                                      \
                    case 3:                                         \
                        result = tex2D(array##3, uv);               \
                        break;                                      \
                    default:                                        \
                        result = float4(1, 0, 1, 1);                \
                        break;                                      \
                }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float4 tcs[MAX_SLICES];
                tcs[0] = tex2D(_Coordinates0, i.uv);
                tcs[1] = tex2D(_Coordinates1, i.uv);
                tcs[2] = tex2D(_Coordinates2, i.uv);
                tcs[3] = tex2D(_Coordinates3, i.uv);
    
                int index = 0;
                float4 tmp, tc = tcs[0];
                for (int slice = 1; slice < SLICES; slice++)
                {
                    tmp = tcs[slice];
                    if (tmp.a != 0 && (tc.a == 0 || tmp.b < tc.b))
                    {
                        index = slice;
                        tc = tcs[slice];
                    }
                }
    
                float4 col = tex2D(_MainTex, i.uv);
                if (tc.a == 0) // col.a == 1 with clear flags as solid color
                {
                    return col;
                }
                
                float2 uv = tc.xy;
                SAMPLE_PSEUDO_ARRAY(_Input, uv, index, col);
                return col;
            }
            ENDCG
        }
    }
}
