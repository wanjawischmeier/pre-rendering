Shader"Hidden/ComputeWorldSpacePosition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
Cull Off
ZWrite Off
ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

#include "UnityCG.cginc"

#define ARRAY_SIZE 3
#define N_VALUES_TO_SORT 3

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

v2f vert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
}
            
float2 vectorToLonLat(float3 vec)
{
    return float2(
                    atan2(vec.x, vec.z),
                    -acos(vec.y / length(vec))
                );
}
            
float getRaySphereDistance(float3 a, float3 b, float3 c)
{
    float numerator = length(cross(c - a, c - b));
    return numerator / length(b - a);
}

uniform sampler2D _CameraDepthTexture, _Input0, _Input1, _Input2;
uniform float PI;
uniform float3 PCAM, P0, P1, P2;
uniform float3 POINTS[ARRAY_SIZE];
uniform float4x4 _ViewProjInv;
float2 tc[ARRAY_SIZE];

fixed4 frag(v2f i) : SV_Target
{
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
    if (depth == 0)
    {
        depth = 1;
    }
    
    float4 clipPos = float4(i.uv * 2.0 - 1.0, depth, 1.0);
    float4 worldPos = mul(_ViewProjInv, clipPos);
    float3 P = worldPos.xyz / worldPos.w;
    float2 ll = vectorToLonLat(P - PCAM);
    if (depth != 1)
    {
        return fixed4(P, 1);
    }
                
                [unroll]
    for (int i = 0; i < ARRAY_SIZE; ++i)
    {
        tc[i] = float2(getRaySphereDistance(PCAM, P, POINTS[i]), i);
    }
                /*
                // Insertion sort for the smallest N values based on z component
                [unroll]
                for (i = 1; i < N_VALUES_TO_SORT; ++i)
                {
                    float4 key = tc[i];
                    int j = i - 1;
                    
                    [unroll]
                    while (j >= 0 && tc[j].z > key.z)
                    {
                        tc[j + 1] = tc[j];
                        --j;
                    }
                    
                    tc[j + 1] = key;
                }
                */
    float2 second, lowest = tc[0];
                [unroll]
    for (i = 1; i < ARRAY_SIZE; ++i)
    {
        if (tc[i].x < lowest.x)
        {
            second = lowest;
            lowest = tc[i];
        }
    }
                // lowest = second;
                
    float2 tc = float2(ll.x / (PI * 2) + 0.5, ll.y / PI);
    fixed4 col;
    switch (lowest.y)
    {
        case 0:
            col = tex2D(_Input0, tc);
            break;
        case 1:
            col = tex2D(_Input1, tc);
            break;
        case 2:
            col = tex2D(_Input2, tc);
            break;
        default:
            col = fixed4(1, 0, 1, 1);
            break;

    }
    return col;
}
            ENDCG
        }
    }
}
