Shader "Hidden/DownhillSimplexAbstract"
{
    Properties { }
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

            float PI, PI2, FAC, OFF;
            float2 X0, X1, X2, TGT;
            float3 OFFSET;

            float objective(float2 uv)
            {
                return length(uv - TGT);
            }

            float2 downhillSimplex(float2 x0, float2 x1, float2 x2)
            {
                // initialization
                float3 b = float3(x0, objective(x0));
                float3 g = float3(x1, objective(x1));
                float3 w = float3(x2, objective(x2));


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
                    r.z = objective(r.xy);

                    if (r.z < g.z)
                        w = r;
                    
                    else
                    {
                        if (r.z < w.z)
                            w = r;

                        float3 h;
                        h.xy = (w.xy + m.xy) / 2.0; // try int 2
                        h.z = objective(h.xy);

                        if (h.z < w.z)
                            w = h;
                    }


                    // expansion
                    if (r.z < b.z)
                    {
                        float3 e;
                        e.xy = m.xy + GAMMA * (r.xy - m.xy);
                        e.z = objective(e.xy);

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
                        c.z = objective(c.xy);

                        if (c.z < w.z)
                            w = c;
                    }
                }

                return b.xy;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // fixed4 col = tex2D(_MainTex, i.uv);
                float err = objective(i.uv);
                float2 opt = downhillSimplex(i.uv, X1, X2);
                
                fixed4 col = fixed4(opt.xy * FAC + OFF, tan(1 - opt.x), 1);
                return col;
            }
            ENDCG
        }
    }
}
