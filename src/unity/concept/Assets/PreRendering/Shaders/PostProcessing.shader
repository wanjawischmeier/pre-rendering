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

			struct circularSamples
			{
				float4 s0, s1, s2, s3, s4, s5, s6, s7, s8;
			};
			
			float PI, PI2;
			
			float2 gnomonicProjection(float2 pos, float fov, float phi1, float lambda0)
			{
				float x = PI2 * (pos.x - 0.5);
				float y = PI * (pos.y - 0.5);

				float p = sqrt(x * x + y * y);
				float c = atan2(p, fov);

				float sinC = sin(c); float cosC = cos(c);
				float sinPhi1 = sin(phi1); float cosPhi1 = cos(phi1);

				float phi = asin(cosC * sinPhi1 + y * sinC * cosPhi1 / p);
				float lambda = lambda0 + atan2(x * sinC, (p * cosPhi1 * cosC - y * sinPhi1 * sinC));

				return float2(lambda / PI2 + 0.5, phi / PI + 0.5);
			}

			circularSamples sampleCircle(Texture2DArray _Input, SamplerState sampler_Input, float2 tc, float2 texelSize, float index)
			{
				circularSamples s;

				s.s0 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(-texelSize.x, -texelSize.y), index));
				s.s1 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(0,            -texelSize.y), index));
				s.s2 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(texelSize.x,  -texelSize.y), index));
				s.s3 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(-texelSize.x, 0           ), index));
				s.s4 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc									 , index));
				s.s5 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(texelSize.x,  0           ), index));
				s.s6 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(-texelSize.x, texelSize.y ), index));
				s.s7 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(0,            texelSize.y ), index));
				s.s8 = UNITY_SAMPLE_TEX2DARRAY(_Input, float3(tc + float2(texelSize.x,  texelSize.y ), index));

				return s;
			}

			// Based on https://stackoverflow.com/a/26357357/13215204
			float2 calculateNormals(circularSamples s)
			{
				float2 n = float2(
					-(s.s2.a - s.s0.a + 2 * (s.s5.a - s.s3.a) + s.s8.a - s.s6.a),
					-(s.s6.a - s.s0.a + 2 * (s.s7.a - s.s1.a) + s.s8.a - s.s2.a)
				);

				return normalize(n) * 0.5 + 0.5;
			}

			float4 blur(circularSamples s, float amount)
			{
				float4 blurred = (s.s0 + s.s1 + s.s2 + s.s3 + s.s4 + s.s5 + s.s6 + s.s7 + s.s8) / 9;
				return blurred * amount + s.s4 * (1 - amount);
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			Texture2D<half4> _Projection;
			Texture2DArray<float4> _InputArray;
			SamplerState linear_repeat_sampler;
			SamplerState sampler_InputArray;
			float FOV, FCLIP, CUTOFF, DOF_INTENSITY, MIST_FALLOFF, MIST_OFFSET;
			float2 Rotation, InputArrayRes;
			float3 MIST_COL;
			int Debug, MX_IDX;

			fixed4 frag (v2f i) : SV_Target
			{
				// Projection
				float2 tc = gnomonicProjection(i.uv, FOV, Rotation.x, Rotation.y);
				half4 idx = _Projection.Sample(linear_repeat_sampler, tc);
				idx.z *= MX_IDX;
				idx.z -= 1;

				// Sampling
				float2 texelSize = 1 / InputArrayRes;
				circularSamples s = sampleCircle(_InputArray, sampler_InputArray, idx.xy, texelSize, idx.z);

				// Normals
				float2 n = calculateNormals(s);
				
				// Depth of field
				float2 cIdx = float2(0.5, 0.5) + Rotation.yx / float2(PI2, PI);
				float cDist = UNITY_SAMPLE_TEX2DARRAY(_InputArray, float3(cIdx, idx.z)).a;
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

				return col;
			}
			ENDCG
		}
	}
}
