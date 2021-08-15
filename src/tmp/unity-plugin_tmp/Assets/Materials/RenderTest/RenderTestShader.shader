// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/RenderTestShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _PanTex ("Panorama Texture", 2D) = "white" {}
        _FieldOfView ("FOV", Range(0.5, 10)) = 0.5
        _Phi1("Phi1", Range(-1, 1)) = 0
        _Lambda0("Lambda0", Range(-1, 1)) = 0
        _Theta("Rotation of Tex", Range(0, 1)) = 0
        _XOff ("X Offset", Range(0, 1)) = 0
        _YOff ("Y Offset", Range(0, 1)) = 0
        _ZOff ("Z Offset", Range(0, 1)) = 0
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

            float2 gnomicProjection(float2 pos, float fov, float phi1, float lambda0, float theta, float PI)
            {
                float PI2 = 2 * PI;

                float x = PI2 * (pos.x - 0.5);
                float y = PI * (pos.y - 0.5);

                float p = sqrt(x * x + y * y);
                float c = atan2(p, fov);

                float sinC = sin(c); float cosC = cos(c);
                float sinPhi1 = sin(phi1); float cosPhi1 = cos(phi1);

                float phi = asin(cosC * sinPhi1 + y * sinC * cosPhi1 / p);
                float lambda = lambda0 + atan2(x * sinC, (p * cosPhi1 * cosC - y * sinPhi1 * sinC));

                return float2(lambda / (PI * 2.0) + 0.5, (phi / PI) + 0.5);
            }

            float4x4 getTranslationMatrix(float x, float y, float z)
            {
                return float4x4(
                    1, 0, 0, x,
                    0, 1, 0, y,
                    0, 0, 1, z,
                    0, 0, 0, 1
                    );
            }

            float4x4 getRotationMatrix(float x, float y, float z)
            {
                float sinx = sin(x); float cosx = cos(x);
                float siny = sin(y); float cosy = cos(y);
                float sinz = sin(z); float cosz = cos(z);

                float4x4 xr = float4x4(
                    1, 0, 0, 0,
                    0, cosx, -sinx, 0,
                    0, sinx, cosx, 0,
                    0, 0, 0, 1
                    );
                float4x4 yr = float4x4(
                    cosy, 0, siny, 0,
                    0, 1, 0, 0,
                    -siny, 0, cosy, 0,
                    0, 0, 0, 1
                    );
                float4x4 zr = float4x4(
                    cosz, -sinz, 0, 0,
                    sinz, cosz, 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1
                    );

                return xr * yr * zr;
            }

            v2f vert (appdata v)
            {
                v2f o;
                // Equivalent to mul(UNITY_MATRIX_MVP, v.vertex)
                o.vertex = UnityObjectToClipPos(v.vertex);
                // o.vertex = v.vertex; // mul(UNITY_MATRIX_P, v.vertex);
                // o.vertex = UnityObjectToClipPos(o.vertex);
                // o.overt = v.vertex;
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            sampler2D _PanTex;
            float4x4 _CamToWorld;
            float4x4 _ViewToWorld;
            float4x4 _WorldToCam;
            float4x4 _Translate;
            float _FieldOfView;
            float _Phi1;
            float _Lambda0;
            float _Theta;
            float _XOff;
            float _YOff;
            float _ZOff;

            fixed4 frag(v2f i) : SV_Target
            {
                /*
                // i.uv = mul(UNITY_MATRIX_VP, i.uv);
                float2 projected = gnomicProjection(i.uv, _FieldOfView, _Phi1, _Lambda0, _Theta, 3.14159);
                float depth = tex2D(_PanTex, projected).a;

                float4 pos = float4(projected.x, projected.y, depth, 1);
                float4 uvpos = float4(i.uv.x, i.uv.y, depth, 1);
                // float4 reprojected = mul(_InverseViewProjection, pos);
                // float3 reprojected = UnityObjectToViewPos(pos);

                float4 world = mul(_CamToWorld, uvpos);
                float4 trans = mul(getTranslationMatrix(_XOff, _YOff, _ZOff), world);
                // trans = float4(world.x + _XOff, world.y + _YOff, world.z + _ZOff, world.w);
                float4 camsp = mul(_WorldToCam, trans);
                float4 rp = mul(UNITY_MATRIX_VP, trans);
                float4 cp = UnityObjectToClipPos(camsp);
                float4 sp = ComputeScreenPos(rp);

                float2 reprojected = gnomicProjection(camsp.xy, _FieldOfView, 0, 0, 0, 3.14159); // mul(UNITY_MATRIX_P, camsp);

                fixed4 col = tex2D(_PanTex, camsp);
                fixed4 t = tex2D(_MainTex, i.uv);
                // just invert the colors
                // col.rgb = 1 - col.rgb;
                return i.overt; // float4(reprojected.xy, 0, 0);
                */
                float PI = 3.14159;

                float2 projected = gnomicProjection(i.uv, _FieldOfView, _Phi1, _Lambda0, _Theta, PI);
                float depth = tex2D(_PanTex, projected).a;
                float2 pos = float2(projected.x * depth * _FieldOfView + _XOff, projected.y * depth * _FieldOfView + _YOff);
                
                float2 reprojected = gnomicProjection(pos, _FieldOfView, _Phi1, _Lambda0, _Theta, PI);
                fixed4 col = tex2D(_PanTex, reprojected);
                return col;
            }
            ENDCG
        }
    }
}
