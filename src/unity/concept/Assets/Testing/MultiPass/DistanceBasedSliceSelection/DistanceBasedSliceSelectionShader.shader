Shader "Unlit/DistanceBasedSliceSelectionShader"
{
    Properties
    {
        _Left ("Left", 2D) = "green" {}
        _Right ("Right", 2D) = "red" {}
        _LeftOffset ("Left Offset", Vector) = (0, 0, 0, 1)
        _RightOffset ("Right Offset", Vector) = (0, 0, 0, 1)
        _Left3DOffset ("Left 3D Offset", Vector) = (0, 0, 0, 1)
        _Right3DOffset ("Right 3D Offset", Vector) = (0, 0, 0, 1)
        _Mode ("Mode", Int) = 0
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

            sampler2D _Left, _Right;
            float4 _MainTex_ST, _LeftOffset, _RightOffset, _Left3DOffset, _Right3DOffset;
            int _Mode;

            float3 GetWorldPosition(float2 uv, float depth_normalized, float3 offset)
            {
                float depth = 0.1 + (4.0 - 0.1) * depth_normalized;
                float3 pos = float3((uv * 2 - 1) * depth, depth);
                return pos + offset;
            }

            float2 GetUVFromWorldPosition(float3 pos, float depth_normalized, float3 offset)
            {
                pos -= offset;
                float depth = 0.1 + (4.0 - 0.1) * depth_normalized;
                return ((pos.xy / depth) + 1) / 2;
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
                // float4 left = tex2D(_Left, i.uv * 2 + _LeftOffset.xy);
                // float4 right = tex2D(_Right, i.uv * 2 + _RightOffset.xy);
                float4 left = tex2D(_Left, i.uv);
                // float4 right = tex2D(_Right, i.uv);
    
                float3 pLeft = GetWorldPosition(i.uv, left.a, _Left3DOffset.xyz);
    
                float2 uv = GetUVFromWorldPosition(pLeft, left.a, _Right3DOffset.xyz);
                // float3 pRight = float3(i.uv, right.a) + _Right3DOffset.xyz;
                float4 right = tex2D(_Right, uv + _RightOffset.xy / left.a);

                float3 pRight = GetWorldPosition(i.uv, right.a, _Right3DOffset.xyz);
    
                float dLeft = distance(pLeft, _WorldSpaceCameraPos);
                float dRight = distance(pRight, _WorldSpaceCameraPos);
    
                float mask = dLeft < dRight && pLeft.z != 4;
                
                left.b = mask;
                right.b = mask;
    
                switch (_Mode)
                {
                    case 1:
                        return mask.xxxx;
                        break;
                    case 2:
                        return float4(uv, 0, 1);
                        break;
                    case 3:
                        return right;
                        break;
                    default:
                        return mask == 0 ? left : right;
                        break;
                }
            }
            ENDCG
        }
    }
}
