Shader "Unlit/BilinearDemoInterpolation"
{
    Properties
    {
        _Q00("Q00", Vector) = (0, 0, 0, 0)
        _Q10("Q10", Vector) = (1, 0, 0, 0)
        _Q01("Q01", Vector) = (0, 1, 0, 0)
        _Q11("Q11", Vector) = (1, 1, 0, 0)
        _C00("C00", Color) = (0, 0, 0, 1)
        _C10("C10", Color) = (1, 0, 0, 1)
        _C01("C01", Color) = (0, 1, 0, 1)
        _C11("C11", Color) = (1, 1, 0, 1)
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
            // make fog work
            #pragma multi_compile_fog

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

            float4 bilerp(float2 uv)
            {
                return float4(
                    (1 - uv.x) * (1 - uv.y),
                    uv.x * (1 - uv.y),
                    (1 - uv.x) * uv.y,
                    uv.x * uv.y
                );
            }

            float2 _Q00, _Q10, _Q01, _Q11;
            float3 _C00, _C10, _C01, _C11;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                /*
                float x0 = _Q00.x;
                float y0 = _Q00.y;
                float x1 = _Q11.x;
                float y1 = _Q11.y;
                */
                float x = i.uv.x;
                float y = i.uv.y;
                /*
                float3 w00 = _C00 * (x1 - x) * (y1 - y);
                float3 w10 = _C10 * (x - x0) * (y1 - y);
                float3 w01 = _C01 * (x1 - x) * (y - y0);
                float3 w11 = _C11 * (x - x0) * (y - y0);
                float fac = 1 / ((x1 - x0) * (y1 - y0));
                float3 w = fac * (w00 + w10 + w01 + w11);
                */

                float4 wp = bilerp(i.uv);
                float2 p = _Q00 * wp.x + _Q10 * wp.y + _Q01 * wp.z + _Q11 * wp.w;
                float4 ww = bilerp(i.uv);
                float3 w = _C00 * ww.x + _C10 * ww.y + _C01 * ww.z + _C11 * ww.w;
                
                return float4(w, 1);
            }
            ENDCG
        }
    }
}
