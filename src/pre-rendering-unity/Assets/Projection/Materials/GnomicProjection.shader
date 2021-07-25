Shader "Unlit/GnomicProjection"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _AngleOfView ("Angle Of View", Float) = 0
        _Phi1 ("Phi1", Float) = 0
        _Lambda0("Lambda0", Float) = 0
        _Theta("Rotation of Tex", Float) = 0
        _XOff("X Offset", Float) = 0
        _YOff("Y Offset", Float) = 0
        _ZOff("Z Offset", Float) = 0
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

            sampler2D _MainTex;
            sampler2D _MainTex_ST;
            float _AngleOfView;
            float _Phi1;
            float _Lambda0;
            float _Theta;
            float _XOff;
            float _YOff;
            float _ZOff;

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

            float3x3 getXYTranslationMatrix(float2 translation)
            {
                return float3x3(1, 0, translation.x, 0, 1, translation.y, 0, 0, 1);
            }

            float3x3 getXYRotationMatrix(float theta)
            {
                float s = -sin(theta);
                float c = cos(theta);
                return float3x3(c, -s, 0, s, c, 0, 0, 0, 1);
            }

            float2 applyMatrix(float3x3 m, float2 uv) {
                return mul(m, float3(uv.x, uv.y, 1)).xy;
            }

            float2 rotate(float2 uv, float theta)
            {
                return applyMatrix(
                    getXYTranslationMatrix(float2(0.5, 0.5)),
                    applyMatrix( // rotate
                        getXYRotationMatrix(theta),
                        applyMatrix(
                            getXYTranslationMatrix(float2(-0.5, -0.5)),
                            uv.xy
                        )
                    )
                );
            }

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = ComputeScreenPos(o.vertex);
                /*
                o.uv = rotate(v.uv, _Theta);
                o.uv = TRANSFORM_TEX(o.uv, _MainTex);
                */
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float PI = 3.14159;
                float PI2 = 6.28318;

                float x = PI2 * (i.uv.x - 0.5);
                float y = PI * (i.uv.y - 0.5);

                float p = sqrt(x * x + y * y);

                float c = atan2(p, _AngleOfView + _ZOff);

                float phi = asin(cos(c + _XOff) * sin(_Phi1) + y * sin(c + _XOff) * cos(_Phi1) / p);

                float lambda = _Lambda0 + atan2(x * sin(c + _YOff), (p * cos(_Phi1) * cos(c + _YOff) - y * sin(_Phi1) * sin(c + _YOff)));

                float2 tc = float2(lambda / (PI * 2.0) + 0.5, (phi / PI) + 0.5);

                fixed4 test = fixed4(tc.x, tc.y, 0, 1);

                fixed4 col = tex2D(_MainTex, tc);
                col = fixed4(col.r, col.g, col.b, 1);
                /*
                float z = col.a;


                float c2 = atan2(p, _AngleOfView + _ZOff * z);

                float phi2 = asin(cos(c2) * sin(_Phi1) + y * sin(c2) * cos(_Phi1) / p);

                float lambda2 = _Lambda0 + atan2(x * sin(c), (p * cos(_Phi1) * cos(c) - y * sin(_Phi1) * sin(c)));

                float2 tc2 = float2(lambda2 / (PI * 2.0) + 0.5, (phi2 / PI) + 0.5);


                fixed4 col2 = tex2D(_MainTex, tc2);
                */
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
