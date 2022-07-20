Shader "Hidden/PostRasterization"
{
    Properties
    {
        _MainTex0 ("Texture1", 2D) = "white" {}
        _MainTex1 ("Texture2", 2D) = "white" {}
        _DepthTex0 ("Depth1", 2D) = "white" {}
        _DepthTex1 ("Depth2", 2D) = "white" {}
        _InterpTex0 ("Interpolation1", 2D) = "white" {}
        _InterpTex1 ("Interpolation2", 2D) = "white" {}
        _ProjTex0 ("Projection1", 2D) = "white" {}
        _ProjTex1 ("Projection2", 2D) = "white" {}
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
            #define TOLERANCE 0.1

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

            sampler2D _MainTex0, _MainTex1, _DepthTex0, _DepthTex1, _InterpTex0, _InterpTex1, _ProjTex0, _ProjTex1;
            float PI, PI2, FOV, NCLIP, FCLIP, CAM_FCLIP;
            float4 BACK_COL;
            int DEBUG;
            float4x4 TR;

            fixed4 frag (v2f i) : SV_Target
            {
                float x = PI2 * (i.uv.x - 0.5);
                float y = PI * (i.uv.y - 0.5);

                float p = sqrt(x * x + y * y);
                float c = atan2(p, FOV);
                
                float sinC = sin(c);

                // simplified gnomonic projection
                float phi = asin(y * sinC / p);
                float lambda = atan2(x * sinC, p * cos(c));
                
                float2 tc = float2(lambda / PI2 + 0.5, phi / PI + 0.5);
                // tc = tc < 0 ? 1 - abs(tc) % 1 : tc % 1;

                fixed4 pc0 = tex2D(_ProjTex0, tc);
                fixed4 pc1 = tex2D(_ProjTex1, tc);
                /*
                if (!any(pc.xy) || true)
                {
                    float2 ll1 = tc.yx;
                    ll1.x *= PI;
                    ll1.y *= PI2;
                    ll1.y += PI;

                    float4 P = float4(
                        sin(ll1.y) * sin(ll1.x),
                        cos(ll1.x),
                        cos(ll1.y) * sin(ll1.x),
                        1
                    );

                    P = mul(TR, P);

                    float2 ll2 = float2(
                        acos(P.y),
                        atan2(P.x, P.z)
                    );

                    pc = fixed4(
                        ll2.y / PI2,
                        ll2.x / PI,
                        0, 1
                    );
                    pc.x += 0.5;
                }
                */


                fixed4 col, db;

                // no tex 0 coords
                if (!any(pc0.xy))
                {
                    col = tex2D(_MainTex1, pc1.xy);
                    db = fixed4(1, 0, 0, 1);
                }

                // no tex 1 coords
                else if (!any(pc1.xy))
                {
                    col = tex2D(_MainTex0, pc0.xy);
                    db = fixed4(0, 1, 0, 1);
                }
                
                // both coords available
                else
                {
                    float d0 = tex2D(_DepthTex0, tc).x;
                    float d1 = tex2D(_DepthTex1, tc).x;
                    float d = d0 - TOLERANCE - d1;

                    // d0 closer
                    if (d < 0)
                    {
                        float r = abs(d) / TOLERANCE;
                        col = r * tex2D(_MainTex0, pc0.xy) + (1 - r) * tex2D(_MainTex1, pc1.xy);
                        db = fixed4(r, r, r, 1);
                    }

                    // d1 closer
                    else
                    {
                        float r = d / TOLERANCE;
                        col = (1 - r) * tex2D(_MainTex0, pc0.xy) + r * tex2D(_MainTex1, pc1.xy);
                        db = fixed4(r, r, r, 1);
                    }

                    /*
                    if (d1 < d2 + TOLERANCE)
                    {
                        // return fixed4(0, pc1.ba, 1);
                        col = tex2D(_MainTex1, pc1.xy);
                        db = (pc1.x > 0.9 && pc1.y > 0.9 && d1 < 6) ? fixed4(1, 1, 1, 1) : fixed4(pc1.xy, 0, 1);
                    }
                    // else if (d2 != CAM_FCLIP)
                    else
                    {
                        // return fixed4(0, pc2.ba, 1);
                        col = tex2D(_MainTex2, pc2.xy);
                        db = (pc2.x > 0.9 && pc2.y > 0.9 && d2 < 6) ? fixed4(1, 1, 1, 1) : pc2;
                    }
                    else
                    {
                        col = BACK_COL;
                    }*/
                }

                return DEBUG ? db : col;
            }
            ENDCG
        }
    }
}
