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

            #define ITERATIONS 20
            // #define LEARNING_RATE 1
            // #define ADAPTIVE_LEARNING 0.5

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
            float PI, PI2, FAC, OFF, LEARNING_RATE, ADAPTIVE_LEARNING;
            float2 TGT;
            float3 OFFSET;
            
            float2 uv2ll(float2 uv)
            {
                return float2(
                    uv.y * PI,
                    uv.x * PI2 + PI
                );
            }

            float2 ll2uv(float2 latLon)
            {
                return float2(
                    latLon.y / PI2 + 0.5,
                    latLon.x / PI
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

                float d = length(P);

                return float2(
                    acos(P.y / d),
                    atan2(P.x, P.z)
                );
            }

            float2 translateUV(float2 uv, float3 translation, float dist = 1)
            {
                float2 ll0 = uv2ll(uv);
                float2 ll1 = translateLatLon(ll0, translation, dist);
                return ll2uv(ll1);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                /*
                float2 ll0 = uv2ll(i.uv);
                float2 initial_guess = translateLatLon(ll0, OFFSET);
                float d = tex2D(_MainTex, ll2uv(initial_guess)).a;
                float2 reprojection = translateLatLon(initial_guess, -OFFSET, d);
                float err = length(initial_guess - reprojection);
                float prev_err = err;
                
                float2 idx = initial_guess;
                */
                /*
                [unroll(ITERATIONS)] for (int i = 0; i < ITERATIONS; i++)
                {
                    float2 ll1 = translateLatLon(ll0, OFFSET);
                    float d1 = tex2D(_MainTex, ll2uv(ll1)).a;
                    float2 ll2 = translateLatLon(ll1, -OFFSET, d1);
                    err = length(abs(ll0 - ll2));
                    float slope = err - prev_err;
                    idx -= slope * LEARNING_RATE;
                }
                */
                /*
                float d = tex2D(_MainTex, i.uv).a;
                float2 llt = uv2ll(TGT);
                float2 ll1 = translateLatLon(ll0, OFFSET, d);
                float err = length(ll1 - llt);

                if (err < 0.2)
                    return fixed4(1, 0, 1, 1);

                // fixed4 col = tex2D(_MainTex, ll2uv(ll1));
                // fixed4 col = fixed4(length(initial_guess - reprojection).xxx, 1);
                fixed4 col = fixed4(err.xxx, 1);
                
                col *= FAC;
                col += OFF;
                
                float t = 0;

                [unroll(ITERATIONS)] for (int i = 0; i < ITERATIONS; i++)
                {
                    t += tex2D(_MainTex, ll2uv(ll0) + float2(i / (float)2, 0)).a;
                    t -= tex2D(_MainTex, ll2uv(ll0) + float2(i / (float)2, t)).a;
                }

                return fixed4(col.rgb, t);
                */
                /*
                if (length(TGT - i.uv.xy) < 0.01)
                    return fixed4(0, 1, 1, 1);
                */

                float adaptive_learning_rate = LEARNING_RATE;
                float2 uv_tmp = translateUV(i.uv, -OFFSET);
                float2 previous_gradient = float2(1, 1);
                float c_iter = 0;

                float d;
                float2 uv0, uv1, gradient;

                [unroll(ITERATIONS)] for (int j = 0; j < ITERATIONS; j++)
                {
                    d = tex2D(_MainTex, uv_tmp).a;
                    uv1 = translateUV(uv_tmp, -OFFSET, d);
                    gradient = i.uv - uv1;

                    if (length(gradient) <= length(previous_gradient))
                    {
                        previous_gradient = gradient;
                        uv0 = uv_tmp;
                        uv_tmp -= gradient * adaptive_learning_rate;

                        c_iter = j;
                    }
                    else
                    {
                        uv_tmp = uv0;
                        adaptive_learning_rate *= ADAPTIVE_LEARNING;
                    }
                }

                d = tex2D(_MainTex, i.uv).a;
                float2 uv2 = translateUV(i.uv, -OFFSET, d);
                float2 gradient2 = TGT - uv2;

                fixed4 col = tex2D(_MainTex, uv0);
                d = col.a;
                float2 uv3 = translateUV(uv0, -OFFSET, d);
                float2 gradient3 = TGT - uv3;

                float improvement = length(gradient2) - length(gradient3);
                
                // return fixed4(improvement.xxx, 1);
                // return fixed4(uv0, 0, 1);
                return col;
                // return (c_iter / ITERATIONS).xxxx;
            }
            ENDCG
        }
    }
}
