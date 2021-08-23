// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable
// Upgrade NOTE: replaced '_CameraToWorld' with 'unity_CameraToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/TextCoords"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
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
				float3 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 uv : TEXCOORD0;
				float3 ray : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = ComputeScreenPos(o.pos);
				o.ray = UnityObjectToClipPos(v.vertex).xyz * float3(-1, -1, 1);
				
				o.ray = lerp(o.ray, v.texcoord, v.texcoord.z != 0);

				return o;
			}


			sampler2D _CameraDepthTexture;
			float3 _clipPoint;
			float3 _clipNormal;


			half4 frag(v2f i) : SV_Target
			{
				i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
				float2 uv = i.uv.xy / i.uv.w;

				float depth = UNITY_SAMPLE_DEPTH(tex2D(_CameraDepthTexture, uv));
				depth = Linear01Depth(depth);
				float4 vpos = float4(i.ray * depth,1);
				float3 wpos = mul(unity_CameraToWorld, vpos).xyz;

				return half4(wpos,1.0f);
			}
			ENDCG
		}
	}
}
