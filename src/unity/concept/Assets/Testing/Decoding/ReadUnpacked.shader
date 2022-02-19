Shader "Hidden/ReadUnpacked"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
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

            static const uint PACKED_SIZE = 3;
            static const uint PIXELS_PER_PACK = 4;
            static const uint BYTES_PER_PIXEL = 8;
            static const uint PERCISION = 0xFF;     // 0xFF = 2^8

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

            struct packed4
            {
                uint p[PACKED_SIZE];
            };

            struct unpacked4
            {
                fixed4 p[PIXELS_PER_PACK];
            };

            StructuredBuffer<packed4> InputBuffer;
            float2 Resolution;
            float2 TexelOffset;

            half unpackSingle(packed4 val, inout uint idx)
            {
                uint pid = idx / PIXELS_PER_PACK;                   // pixel index
                uint sid = idx % PIXELS_PER_PACK * BYTES_PER_PIXEL; // shift index
                idx++;

                half res = (half)((val.p[pid] >> sid) & PERCISION) / PERCISION;
                return res;
            }

            unpacked4 unpack(packed4 packed)
            {
                unpacked4 unpacked;
                half r, g, b;
                uint idx = 0;

                [unroll(PIXELS_PER_PACK)] for (uint i = 0; i < PIXELS_PER_PACK; i++)
                {
                    r = unpackSingle(packed, idx);
                    g = unpackSingle(packed, idx);
                    b = unpackSingle(packed, idx);

                    unpacked.p[i] = fixed4(r, g, b, 1);
                }

                return unpacked;
            }

            fixed4 samplePackedBuffer(float2 uv)
            {
                int2 tc = float2(uv.x, 1 - uv.y) * Resolution;
                uint idx = tc.x + tc.y * Resolution.x;      // image pixel index
                uint gid = floor(idx / PIXELS_PER_PACK);    // packed global index
                uint lid = idx % PIXELS_PER_PACK;           // packed local index

                packed4 ppx = InputBuffer[gid];
                unpacked4 upx = unpack(ppx);
                return upx.p[lid];
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = samplePackedBuffer(i.uv);
                
                return col;
            }
            ENDCG
        }
    }
}
