Shader "Hidden/NormalProjection"
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

            #define NCLIP 0.1
            #define FCLIP 30

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
            float PI, PI2, XDIST, YDIST;
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

            float3 ll2vec(float2 latLon, float dist = 1)
            {
                return float3(
                    dist * sin(latLon.y) * sin(latLon.x),
                    dist * cos(latLon.x),
                    dist * cos(latLon.y) * sin(latLon.x)
                );
            }
            
            float2 vec2ll(float3 vec, float dist = 1)
            {
                return float2(
                    acos(vec.y / dist),
                    atan2(vec.x, vec.z)
                );
            }

            float magnitude(float3 vec)
            {
                return sqrt(
                    vec.x * vec.x +
                    vec.y * vec.y +
                    vec.z * vec.z
                );
            }

            float2 translateLatLon(float2 latLon, float3 translation, float3 n, float dist = 1)
            {
                float3 P = ll2vec(latLon, dist);
                P += translation;

                float d = magnitude(P);
                return vec2ll(P, d);
            }

            float2 project(float2 uv, float3 offset)
            {
                float2 CID = uv;
                float2 TID = CID - float2(0, YDIST);
                float2 BID = CID + float2(0, YDIST);
                float2 LID = CID - float2(XDIST, 0);
                float2 RID = CID + float2(XDIST, 0);

                float CD = tex2D(_MainTex, CID).a * (FCLIP - NCLIP) + NCLIP;
                float TD = tex2D(_MainTex, TID).a * (FCLIP - NCLIP) + NCLIP;
                float BD = tex2D(_MainTex, BID).a * (FCLIP - NCLIP) + NCLIP;
                float LD = tex2D(_MainTex, LID).a * (FCLIP - NCLIP) + NCLIP;
                float RD = tex2D(_MainTex, RID).a * (FCLIP - NCLIP) + NCLIP;

                float2 CLL = uv2ll(CID);
                float2 TLL = uv2ll(TID);
                float2 BLL = uv2ll(BID);
                float2 LLL = uv2ll(LID);
                float2 RLL = uv2ll(RID);

                float3 CP = ll2vec(CLL, CD);
                float3 TP = ll2vec(TLL, TD) - CP;
                float3 BP = ll2vec(BLL, BD) - CP;
                float3 LP = ll2vec(LLL, LD) - CP;
                float3 RP = ll2vec(RLL, RD) - CP;

                float3 n0 = cross(TP, RP);
                float3 n1 = cross(RP, BP);
                float3 n2 = cross(BP, LP);
                float3 n3 = cross(LP, TP);
                float3 n = normalize(n0 + n1 + n2 + n3);

                float2 ll1 = uv2ll(uv);
                float2 ll2 = translateLatLon(ll1, offset, n, CD);
                return ll2uv(ll2);
            }



            fixed4 frag (v2f i) : SV_Target
            {
                float2 pc = project(i.uv.xy, POSITION);

                fixed4 col = tex2D(_MainTex, pc);
                return col;
            }
            ENDCG
        }
    }
}
