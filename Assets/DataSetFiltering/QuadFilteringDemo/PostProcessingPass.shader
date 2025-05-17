Shader "Unlit/PostProcessingPass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            Texture2D<int> _DepthBuffer_SW;
            Texture2D<float4> _DepthBuffer_HW;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Berechne Pixelposition aus der Vertexposition
                uint2 pixelPos = uint2(i.vertex.xy);
                
                // Lade Wert aus dem Software-Tiefenpuffer
                int swDepth = _DepthBuffer_SW.Load(int3(pixelPos, 0));
                
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
                    return _DepthBuffer_HW.Load(int3(pixelPos, 0));
                }
            }
            ENDCG
        }
    }
}
