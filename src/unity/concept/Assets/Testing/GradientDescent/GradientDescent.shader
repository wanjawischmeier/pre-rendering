Shader "Hidden/GradientDescent"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            #define ITERATIONS 1
            #define LEARNING_RATE 0.01

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

            sampler2D _MainTex;
            float PI, PI2, F;
            float3 POSITION;

            float2 uv2ll(float2 uv)
            {
                return float2(
                    uv.y * PI,
                    uv.x * PI2 + PI
                );
            }

            float2 ll2uv(float2 ll)
            {
                return float2(
                    ll.y / PI2 + 0.5,
                    ll.x / PI
                );
            }

            float2 translateLatLon(float2 latLon, float3 translation, float dist = 1)
            {
                float3 P = float3(
                    dist * sin(latLon.y) * sin(latLon.x),
                    dist * cos(latLon.x),
                    dist * cos(latLon.y) * sin(latLon.x)
                );

                P += translation;

                return float2(
                    acos(P.y / length(P)),
                    atan2(P.x, P.z)
                );
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 ll0 = uv2ll(i.uv);
                float2 initial_guess = translateLatLon(ll0, -POSITION);
                float d = tex2D(_MainTex, ll2uv(initial_guess)).a;
                float2 reprojection = translateLatLon(initial_guess, POSITION, d);
                float err = length(abs(initial_guess - reprojection));
                float prev_err = err;

                float2 idx = initial_guess;

                [unroll(ITERATIONS)] for (int i = 0; i < ITERATIONS; i++)
                {
                    float2 ll1 = translateLatLon(ll0, -POSITION);
                    float d1 = tex2D(_MainTex, ll2uv(ll1)).a;
                    float2 ll2 = translateLatLon(ll1, POSITION, d1);
                    err = length(abs(ll0 - ll2));
                    float slope = err - prev_err;
                    idx -= slope * LEARNING_RATE;
                }


                
                // fixed4 col = tex2D(_MainTex, ll2uv(ll1));
                // fixed4 col = fixed4(ll2uv(ll2), 0, 1);
                fixed4 col = fixed4(err, err, err, 1);
                
                
                
                return col;
            }
            ENDCG
        }
    }
}
