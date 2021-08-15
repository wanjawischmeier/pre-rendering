Shader "Unlit/StereographicProjection"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

        _AngleOfView("Angle Of View", Float) = 0
        _Phi1("Phi1", Float) = 0
        _Lambda0("Lambda0", Float) = 0
        _Theta("Rotation of Tex", Float) = 0
        _R("Radius", Float) = 0
        _XOff("X Offset", Range(-1, 1)) = 0
        _YOff("Y Offset", Float) = 0
        _ZOff("Z Offset", Float) = 0
        _1Off("1 Offset", Float) = 0
        _2Off("2 Offset", Float) = 0
        _3Off("3 Offset", Float) = 0
        _4Off("4 Offset", Float) = 0
        _5Off("5 Offset", Float) = 0
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _AngleOfView;
            float _Phi1;
            float _Lambda0;
            float _Theta;
            float _R;
            float _XOff;
            float _YOff;
            float _ZOff;
            float _1Off;
            float _2Off;
            float _3Off;
            float _4Off;
            float _5Off;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float PI = 3.14159;
                float PI2 = 6.28318;

                float x = PI2 * (i.uv.x - 0.5);
                float y = PI * (i.uv.y - 0.5);

                float p = sqrt(x * x + y * y);
                float c = 2 * atan2(p / (2 * _R), _AngleOfView);

                float phi = asin(cos(c + _1Off) * sin(_Phi1) + y * sin(c + _2Off) * cos(_Phi1) / p);

                float lambda = _Lambda0 + atan2(x * sin(c + _3Off), (p * cos(_Phi1) * cos(c + _4Off) - y * sin(_Phi1) * sin(c + _5Off)));

                float2 tc = float2(lambda / (PI * 2.0) + 0.5, (phi / PI) + 0.5);
                fixed4 tex = fixed4(tc.x, tc.y, 0, 1);
                fixed4 col = tex2D(_MainTex, tc);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
