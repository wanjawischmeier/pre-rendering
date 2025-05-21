Shader "Unlit/CombinedPass"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma multi_compile _ _DEBUG_WIREFRAME
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

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

            uniform int2 _OutputResolution;

            Texture2D<int> _DepthBuffer_SW;
            Texture2D<float4> _DebugBuffer_SW;
            Texture2D<float> _DepthBuffer_HW;
            Texture2D<float4> _DebugBuffer_HW;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                int3 tc = int3(i.uv * _OutputResolution, 0); // Texture coordinates in uint3 format

                #ifdef _DEBUG_WIREFRAME

                float4 debug = _DebugBuffer_SW.Load(tc);
                if (debug.a == 0.0f && false)
                {
                    debug = _DebugBuffer_HW.Load(tc);
                }

                return debug;

                #else
                
                // Lade Wert aus dem Software-Tiefenpuffer
                int swDepth = _DepthBuffer_SW.Load(tc);
                float depth;
                
                // Prüfe ob er gleich int.MaxValue ist
                if (swDepth < 2147483647) // int.MaxValue
                {
                    depth = asfloat(swDepth);
                    
                }
                else
                {
                    depth = _DepthBuffer_HW.Load(tc);
                }
                
                return fixed4(depth.xxx, 1);

                #endif
            }
            ENDCG
        }
    }
}
