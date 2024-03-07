Shader "Unlit/CubemapStencilApproximation"
{
    SubShader
    {
        Tags { "Queue" = "Transparent" }

        Pass
        {
            ZWrite Off
            ZTest Greater
            Stencil
            {
                Ref 1
                Comp Equal
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Your fragment shader code here
                return fixed4(1, 1, 1, 1); // Example: returning white color
            }
            ENDCG
        }
    }
}
