Shader "Hidden/CombinedTesting"
{
    Properties
    {
        _TexXP("Texture XP", 2D) = "white" {}
        _TexXN("Texture XN", 2D) = "white" {}
        _TexYP("Texture YP", 2D) = "white" {}
        _TexYN("Texture YN", 2D) = "white" {}
        _Phi1("Latitude", Range(-0.5, 0.5)) = 0
        _Lambda0("Longitude", Range(-0.5, 0.5)) = 0
        _Fov("Field Of View", Range(0.1, 10)) = 0
        _X1("X1", Range(-1, 1)) = 0
        _Y1("Y1", Range(-1, 1)) = 0
        _Z1("Z1", Range(-1, 1)) = 0
        _X2("X2", Range(-1, 1)) = 0
        _Y2("Y2", Range(-1, 1)) = 0
        _Z2("Z2", Range(-1, 1)) = 0
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
            #include "Utility/Projection.cginc"

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
            

            sampler2D _TexXP, _TexXN, _TexYP, _TexYN;
            float _Phi1;
            float _Lambda0;
            float _Fov;
            float _X1, _Y1, _Z1, _X2, _Y2, _Z2;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                float PI = 3.14159;
                latlon rotation;
                rotation.lat = _Phi1;
                rotation.lon = _Lambda0;
                rotation = normalizedToLatLon(rotation, PI);

                latlon tc = gnomicProjection(i.uv, _Fov, rotation, PI);

                // latlon tr1 = translateInsideSphere(tc, float3(_X1, _Y1, _Z1), 1, PI);
                // latlon tr2 = translateInsideSphere(tc, float3(_X2, _Y2, _Z2), 1, PI);
                
                fixed d1 = tex2D(_TexXP, toFloat2(tc)).a;
                fixed d2 = tex2D(_TexXN, toFloat2(tc)).a;

                latlon tr1 = translateInsideSphere(tc, float3(_X1, _Y1, _Z1), d1, PI);
                latlon tr2 = translateInsideSphere(tc, float3(_X2, _Y2, _Z2), d2, PI);

                fixed4 col1 = tex2D(_TexXP, toFloat2(tr1));
                fixed4 col2 = tex2D(_TexXN, toFloat2(tr2));

                float4 sw = float4(0, 0, 0, 1);
                float4 col = sw;

                if (col1.a < col2.a) sw.r = 1;
                if (col1.a > col2.a) sw.g = 1;
                if (col1.a > col2.a) col = col1;
                if (col1.a < col2.a) col = col2;

                // return toFloat4(tr);
                // return col1/2+col2/2;
                return col;
            }
            ENDCG
        }
    }
}
