Shader "Unlit/HardwareRasterDebug"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
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

            Texture2D<float> _InputDepthBuffer;
            Texture2D<float4> _InputDebugBuffer;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                int2 tc = i.uv * _OutputResolution;

                #ifdef _DEBUG_WIREFRAME

                // sample the debug buffer
                return _InputDebugBuffer[tc];

                #else

                // sample the depth buffer
                float depth = _InputDepthBuffer[tc];

                return float4(depth.xxx, 1);
                #endif
            }
            ENDCG
        }
    }
}
