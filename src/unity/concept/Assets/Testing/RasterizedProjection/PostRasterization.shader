Shader "Hidden/PostRasterization"
{
    Properties
    {
        _MainTex1 ("Texture1", 2D) = "white" {}
        _MainTex2 ("Texture2", 2D) = "white" {}
        _DepthTex1 ("Depth1", 2D) = "white" {}
        _DepthTex2 ("Depth2", 2D) = "white" {}
        _InterpTex1 ("Interpolation1", 2D) = "white" {}
        _InterpTex2 ("Interpolation2", 2D) = "white" {}
        _ProjTex1 ("Projection1", 2D) = "white" {}
        _ProjTex2 ("Projection2", 2D) = "white" {}
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

            sampler2D _MainTex1, _MainTex2, _DepthTex1, _DepthTex2, _InterpTex1, _InterpTex2, _ProjTex1, _ProjTex2;
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

                fixed4 pc1 = tex2D(_ProjTex1, tc);
                fixed4 pc2 = tex2D(_ProjTex2, tc);
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

                if (!any(pc1.xy))
                {
                    col = tex2D(_MainTex2, pc2.xy);
                    db = fixed4(1, 0, 0, 1);
                }
                else if (!any(pc2.xy))
                {
                    col = tex2D(_MainTex1, pc1.xy);
                    db = fixed4(0, 1, 0, 1);
                }
                
                float d1 = tex2D(_DepthTex1, tc).x;
                float d2 = tex2D(_DepthTex2, tc).x;

                if (d1 < d2 + TOLERANCE)
                {
                    col = tex2D(_MainTex1, pc1.xy);
                    db = (pc1.x > 0.9 && pc1.y > 0.9 && d1 < 6) ? fixed4(1, 1, 1, 1) : fixed4(pc1.xy, 0, 1);
                }
                // else if (d2 != CAM_FCLIP)
                else
                {
                    col = tex2D(_MainTex2, pc2.xy);
                    db = (pc2.x > 0.9 && pc2.y > 0.9 && d2 < 6) ? fixed4(1, 1, 1, 1) : pc2;
                }/*
                else
                {
                    col = BACK_COL;
                }*/

                return DEBUG ? db : col;
            }
            ENDCG
        }
    }
}
