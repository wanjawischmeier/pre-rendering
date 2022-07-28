Shader "Hidden/DownhillSimplexDemo"
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

            #define ALPHA 1
            #define BETA 0.5
            #define GAMMA 2
            #define ITERATIONS 10

            #define EQUILATERAL_TRIANGLE_CONST 1 - 2 / 15.0 // roughly 0,8666

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
            float PI, PI2, FAC, OFF, TRIANGLE_CENTROID_RADIUS;
            float2 X0, X1, X2, TGT;
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

            /*
            float objective(float2 uv)
            {
                return length(uv - TGT);
            }
            */
            float objective(float2 uv, float2 uv0)
            {
                /*
                d: float = img[x.y % height, x.x % width, 3] / float(0xFFFF)

                ll0 = uv2ll(x)
                ll1 = translateLatLon(ll0, offset, d)
                llt = uv2ll(optimum)

                return float2.magnitude(ll1 - llt) * 0.4
                */

                float d = tex2D(_MainTex, uv).a;

                float2 uv1 = translateUV(uv, -OFFSET, d);

                return length(uv0 - uv1);
            }

            float2 downhillSimplex(float2 x0, float2 x1, float2 x2, float2 uv0)
            {
                // initialization
                float3 b = float3(x0, objective(x0, uv0));
                float3 g = float3(x1, objective(x1, uv0));
                float3 w = float3(x2, objective(x2, uv0));


                [unroll(ITERATIONS)] for (int i = 0; i < ITERATIONS; i++)
                {
                    // sort
                    float3 t;

                    if (b.z > g.z)
                    {
                        t = g;
                        g = b;
                        b = t;
                    }

                    if (g.z > w.z)
                    {
                        t = g;
                        g = w;
                        w = t;

                        if (b.z > g.z)
                        {
                            t = g;
                            g = b;
                            b = t;
                        }
                    }


                    // midpoint
                    float3 m;
                    m.xy = (g + b) / 2;


                    // reflection
                    float3 r;
                    r.xy = m.xy + ALPHA * (m.xy - w.xy);
                    r.z = objective(r.xy, uv0);

                    if (r.z < g.z)
                        w = r;
                    
                    else
                    {
                        if (r.z < w.z)
                            w = r;

                        float3 h;
                        h.xy = (w.xy + m.xy) / 2.0; // try int 2
                        h.z = objective(h.xy, uv0);

                        if (h.z < w.z)
                            w = h;
                    }


                    // expansion
                    if (r.z < b.z)
                    {
                        float3 e;
                        e.xy = m.xy + GAMMA * (r.xy - m.xy);
                        e.z = objective(e.xy, uv0);

                        if (e.z < r.z)
                            w = e;

                        else
                            w = r;
                    }


                    // contraction
                    if (r.z > g.z)
                    {
                        float3 c;
                        c.xy = m.xy + BETA * (w.xy - m.xy);
                        c.z = objective(c.xy, uv0);

                        if (c.z < w.z)
                            w = c;
                    }
                }

                return b.xy;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (length(TGT - i.uv.xy) < 0.01)
                    return fixed4(0, 1, 1, 1);

                float2 uv1 = translateUV(i.uv.xy, OFFSET);

                float x = EQUILATERAL_TRIANGLE_CONST * TRIANGLE_CENTROID_RADIUS;
                float y = uv1.y - 0.5 * TRIANGLE_CENTROID_RADIUS;

                float2 a = float2(uv1.x - x, y);
                float2 b = float2(uv1.x + x, y);
                float2 c = float2(uv1.x, y + TRIANGLE_CENTROID_RADIUS);

                // fixed4 col = tex2D(_MainTex, i.uv);
                float cost = objective(uv1, i.uv);
                float2 opt = downhillSimplex(a, b, c, i.uv);
                // float err = length(opt - TGT);
                float err = objective(opt, i.uv);
                
                fixed4 col = fixed4(opt.xy * FAC + OFF, tan(1 - opt.x), 1);
                // return fixed4(err, sin(i.uv.xy * err), 1);
                // return fixed4(opt, 0, 1);
                // return err < 0.1 ? tex2D(_MainTex, opt) : cost.xxxx;
                return err.xxxx;
            }
            ENDCG
        }
    }
}
