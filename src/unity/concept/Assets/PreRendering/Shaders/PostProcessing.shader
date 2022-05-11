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
            StructuredBuffer<int> ChunkIndicies;
			float FOV, NCLIP, FCLIP, CUTOFF, DOF_INTENSITY, MIST_FALLOFF, MIST_OFFSET, PLAYER_ICON;
			float2 ROTATION, InputBufferResolution;
			float3 POSITION, POSITION_OFFSET, MIST_COL;
			int DEBUG, IMG_IDX;

			half sampleDepthChannel(float2 tc, int idx)
			{
				fixed3 dpm = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(tc, idx * 2 + 1));
				return unpackDepth(dpm);
			}

			half4 samplePackedArray(float2 tc, float idx)
			{
				fixed3 col = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(tc, idx * 2));
				return half4(col, sampleDepthChannel(tc, idx));
			}

			circularSamples sampleCircle(float2 tc, float2 texelSize, int idx)
			{
			    circularSamples s;
			
			    float2 i0 = tc + float2(-texelSize.x, -texelSize.y);
			    float2 i1 = tc + float2( 0,           -texelSize.y);
			    float2 i2 = tc + float2( texelSize.x, -texelSize.y);
			    float2 i3 = tc + float2(-texelSize.x,  0          );
				float2 i4 = tc;
			    float2 i5 = tc + float2( texelSize.x,  0          );
			    float2 i6 = tc + float2(-texelSize.x,  texelSize.y);
			    float2 i7 = tc + float2( 0,            texelSize.y);
			    float2 i8 = tc + float2( texelSize.x,  texelSize.y);
			
			    s.s0 = samplePackedArray(i0, idx);
			    s.s1 = samplePackedArray(i1, idx);
			    s.s2 = samplePackedArray(i2, idx);
			    s.s3 = samplePackedArray(i3, idx);
			    s.s4 = samplePackedArray(i4, idx);
			    s.s5 = samplePackedArray(i5, idx);
			    s.s6 = samplePackedArray(i6, idx);
			    s.s7 = samplePackedArray(i7, idx);
			    s.s8 = samplePackedArray(i8, idx);
			
			    return s;
			}

			float2 project(float2 tc, int idx, float3 offset)
			{
				float2 ll1 = normalizedToLatLon(tc.yx);

				// half CP = sampleDepthChannel(tc, idx);
				half CP = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(tc, idx)).a;
				CP *= (FCLIP - NCLIP) + NCLIP;

				float2 ll2 = translateLatLon(ll1, offset, CP);
				return latLonToNormalized(ll2);
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
				float2 tc = gnomonicProjection(i.uv, FOV, ROTATION.x, ROTATION.y);
				float2 pc = project(tc, IMG_IDX, POSITION - POSITION_OFFSET);
				/*
				float2 pc0 = project(tc, IMG_IDX, POSITION);
				float2 pc1 = project(tc, IMG_IDX + 4, POSITION - float3(0, 0, 4));
				float2 avg = (pc0 + pc1) / 2;
				*/

				fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(pc, IMG_IDX));

				// Debug
				switch (DEBUG)
				{
				case 1:
					return fixed4(tc, 0, 1);
				case 2:
					return fixed4(pc.xy, 0, 1);
				case 5:
					return fixed4(col.aaa, 1);
				}

				return col;
				/*
				if (magnitude(avg - pc0) < magnitude(avg - pc1))
					return samplePackedArray(pc0, IMG_IDX);
				else
					return samplePackedArray(pc1, IMG_IDX + 4);
				
				// Sampling
				float2 texelSize = 1 / InputBufferResolution;
				circularSamples s = sampleCircle(pc.xy, texelSize, IMG_IDX);

				// Normals
				float2 n = calculateNormals(s);

				// Depth of field
				float2 cIdx = float2(0.5, 0.5) + ROTATION.yx / float2(PI2, PI);
				float cDist = sampleDepthChannel(cIdx, IMG_IDX);
				float dof = abs(cDist - s.s4.a) * DOF_INTENSITY;

				// Debug
				switch(DEBUG)
				{
				case 1:
					return fixed4(tc, 0, 1);
				case 2:
					return fixed4(pc.xy, 0, 1);
				case 3:
					return fixed4(n, 0, 1);
				case 4:
					return fixed4(dof.xxx, 1);
				case 5:
					return fixed4(s.s4.aaa, 1);
				// NOT WORKING!!!
				case 6:
					float ipl = magnitude(abs(tc) - abs(pc));
					return fixed4(ipl, ipl, ipl, 1);
				}

				fixed4 col = blur(s, dof);
				float eDist = pow(clamp(col.a - MIST_OFFSET, 0, 1), MIST_FALLOFF * FCLIP);
				col = MIST_COL.rgbb * eDist + col * (1 - eDist);
				
				return col;
				*/
				// fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(pc, IMG_IDX));
				// fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(i.uv, IMG_IDX));
				/*
				float3 rpos = (POSITION - POSITION_OFFSET) / float3(20, 20, 20);

				if (distance(i.uv, rpos.xz) < PLAYER_ICON)
					col = fixed4(0.5, 1, 1, 1);
				*/
				/*
				fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(i.uv, 0));
				fixed4 dpm = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(i.uv, 1));
				fixed4 dpc = UNITY_SAMPLE_TEX2DARRAY(_InputBuffer, float3(i.uv, 2));

				if (IMG_IDX == 0)
					return contrast(dpm[2], 4).rgbb;

				return contrast(unpackDepth(dpm.bg), 4).rgbb;
				*/
			}
			ENDCG
		}
	}
}