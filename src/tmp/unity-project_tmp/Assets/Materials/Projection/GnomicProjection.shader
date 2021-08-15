Shader "Unlit/GnomicProjection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _AngleOfView ("Angle Of View", Float) = 0
        _Phi1 ("Phi1", Float) = 0
        _Lambda0 ("Lambda0", Float) = 0
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

            // const float PI = 3.14159;
            // const float PI2 = 6.28318;
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _AngleOfView;
            float _Phi1;
            float _Lambda0;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float PI = 3.14159;
                float PI2 = 6.28318;

                float x = PI2 * (i.uv.x - 0.5);  //input texture coordinates, 
                float y = PI2 * (i.uv.y - 0.5);

                float p = sqrt(x * x + y * y);

                float c = atan2(p, _AngleOfView);

                float phi = asin(cos(c) * sin(_Phi1) + y * sin(c) * cos(_Phi1) / p);

                float lambda = _Lambda0 + atan2(x * sin(c), (p * cos(_Phi1) * cos(c) - y * sin(_Phi1) * sin(c)));

                float2 tc = float2(lambda / (PI * 2.0) + 0.5, (phi / PI) + 0.5); //reprojected texture coordinates

                fixed4 test = fixed4(tc.x, tc.y, 0, 1);
                // vec4 texSample = texture2D(tEqui, tc); //sample using new coordinates
                // sample the texture
                fixed4 col = tex2D(_MainTex, tc);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
