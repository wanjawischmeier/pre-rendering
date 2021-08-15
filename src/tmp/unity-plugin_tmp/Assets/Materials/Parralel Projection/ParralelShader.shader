Shader "Unlit/ParralelShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Rotation ("Camera Rotation", Vector) = (0, 0, 0, 0)
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float3 _Rotation;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                float3 a = float3(i.x, i.y, col.a);
                float3 c = float3(0, 0, 0);
                float3 e = float3(1, 1, 1);

                float3 x = a.x - c.x;
                float3 y = a.y - c.y;
                float3 z = a.z - c.z;

                float3 


                return col;
            }
            ENDCG
        }
    }
}
