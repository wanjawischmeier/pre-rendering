Shader "PreRendering/PostProcessing"
{
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
			#include "Helper.cginc"

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

			UNITY_DECLARE_TEX2DARRAY(_InputBuffer);
			Texture2D<half4> _Projection;
			SamplerState linear_repeat_sampler;
			float FOV, FCLIP, CUTOFF, DOF_INTENSITY, MIST_FALLOFF, MIST_OFFSET;
			float2 Rotation, InputBufferResolution;
			float3 MIST_COL;
			int Debug, MX_IDX, FIXED_IDX;

			circularSamples sampleCircle(float2 tc, float2 texelSize, float index)
			{
			    circularSamples s;
			
			    float3 i0 = float3(tc + float2(-texelSize.x, -texelSize.y),  index);
			    float3 i1 = float3(tc + float2( 0,           -texelSize.y),  index);
			    float3 i2 = float3(tc + float2( texelSize.x, -texelSize.y),  index);
			    float3 i3 = float3(tc + float2(-texelSize.x,  0          ),  index);
			    float3 i4 = float3(tc,                                          index);
			    float3 i5 = float3(tc + float2( texelSize.x,  0          ), index);
			    float3 i6 = float3(tc + float2(-texelSize.x,  texelSize.y), index);
			    float3 i7 = float3(tc + float2( 0,            texelSize.y), index);
			    float3 i8 = float3(tc + float2( texelSize.x,   texelSize.y), index);
			
			    s.s0 = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, i0);
			    s.s1 = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, i1);
			    s.s2 = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, i2);
			    s.s3 = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, i3);
			    s.s4 = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, i4);
			    s.s5 = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, i5);
			    s.s6 = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, i6);
			    s.s7 = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, i7);
			    s.s8 = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, i8);
			
			    return s;
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// Projection
				float2 tc = gnomonicProjection(i.uv, FOV, Rotation.x, Rotation.y);
				half4 idx = _Projection.Sample(linear_repeat_sampler, tc);
				idx.z *= MX_IDX;
				idx.z -= 1;

				// Sampling
				float2 texelSize = 1 / InputBufferResolution;
				circularSamples s = sampleCircle(idx.xy, texelSize, idx.z);

				// Normals
				float2 n = calculateNormals(s);
				
				// Depth of field
				float3 cIdx = float3(float2(0.5, 0.5) + Rotation.yx / float2(PI2, PI), idx.z);
				float cDist = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, cIdx);
				float dof = abs(cDist - s.s4.a) * DOF_INTENSITY;

				// Debug
				switch(Debug)
				{
				case 1:
					return fixed4(tc, 0, 1);
				case 2:
					return fixed4(idx.xy, 0, 1);
				case 3:
					return fixed4(n, 0, 1);
				case 4:
					return fixed4(dof.xxx, 1);
				case 5:
					return fixed4(s.s4.aaa, 1);
				}

				fixed4 col = blur(s, dof);
				float eDist = pow(clamp(col.a - MIST_OFFSET, 0, 1), MIST_FALLOFF * FCLIP);
				col = MIST_COL.rgbb * eDist + col * (1 - eDist);

				col = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(i.uv, FIXED_IDX));

				return col;
			}
			ENDCG
		}
	}
}
