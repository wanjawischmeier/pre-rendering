Shader "PreRendering/CubeToEqui"
{
	Properties
	{
		_MainTex ("Texture", CUBE) = "white" {}
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
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float FORWARD;
			float PI;
			v2f vert (float4 v : POSITION,float2 uv : TEXCOORD0)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v);
				o.uv = (uv - .5)*float2(PI*2,PI)+float2(FORWARD,0);
				return o;
			}

			samplerCUBE _MainTex;
			

			fixed4 frag (v2f i) : SV_Target
			{
				float cy = cos(i.uv.y);
				return texCUBE(_MainTex, float3(sin(i.uv.x)*cy, sin(i.uv.y), cos(i.uv.x)*cy));
			}
			ENDCG
		}
	}
}
