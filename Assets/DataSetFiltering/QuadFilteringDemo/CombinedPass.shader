Shader "Unlit/CombinedPass"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

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

            uniform int2 _OutputResolution;

            Texture2D<int> _DepthBuffer_SW;
            Texture2D<float4> _DepthBuffer_HW;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                int3 tc = int3(i.uv * _OutputResolution, 0); // Texture coordinates in uint3 format
                
                // Lade Wert aus dem Software-Tiefenpuffer
                int swDepth = _DepthBuffer_SW.Load(tc);
                
                // Prüfe ob er gleich int.MaxValue ist
                if (swDepth != 2147483647) // int.MaxValue
                {
                    // Konvertiere int zu float4 für die Rückgabe
                    float depth = asfloat(swDepth);
                    return fixed4(depth.xxx, 1);
                }
                else
                {
                    // Gib den Hardware-Tiefenwert zurück
                    return _DepthBuffer_HW.Load(tc);
                }
            }
            ENDCG
        }
    }
}
