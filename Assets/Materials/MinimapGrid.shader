Shader "Custom/MinimapGrid"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0.5, 0.5, 0.5, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
        _MarkedColor ("Marked Color", Color) = (1, 0, 0, 1)
        _PlayerColor ("Player Color", Color) = (0, 1, 0, 1) // Green player marker
        _ViewConeColor ("View Cone Color", Color) = (1, 1, 0, 1) // Yellow view cone
        _GridThickness ("Grid Thickness", Float) = 0.1
        _PlayerPosition ("Player Position", Vector) = (0, 0, 0, 0)
        _PlayerSize ("Player Size", Float) = 0.2
        _PlayerDirection ("Player Direction", Vector) = (0, 1, 0, 0)
        _ViewConeAngle ("View Cone Angle", Float) = 45.0
        _ViewConeLength ("View Cone Length", Float) = 2.0
        _Zoom ("Zoom", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _GridColor;
            float4 _BackgroundColor;
            float4 _MarkedColor;
            float4 _PlayerColor;
            float4 _ViewConeColor;
            float4 _PlayerPosition;
            float _GridThickness;
            float _PlayerSize;
            float2 _PlayerDirection;
            float _ViewConeAngle;
            float _ViewConeLength;
            float _Zoom;

            float4 MarkedCells[10];

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // Ensure one grid cell = 1 world unit, with zoom applied
                i.uv = 1 - i.uv; // Flip UV
                float2 worldPos = ((i.uv - 0.5) * _Zoom) - _PlayerPosition.xy;

                // Grid points only (no lines)
                float gridX = abs(frac(worldPos.x) - 0.5);
                float gridY = abs(frac(worldPos.y) - 0.5);
                float grid = step(gridX, _GridThickness) * step(gridY, _GridThickness);

                // Base color
                float4 color = lerp(_BackgroundColor, _GridColor, grid * _GridColor.a);

                // Marked cells as squares
                for (int j = 0; j < 10; j++)
                {
                    float2 cellCenter = MarkedCells[j].xy;
                    float2 cellDist = abs(worldPos - cellCenter);

                    // Check if worldPos is inside the square cell
                    float isInCell = step(cellDist.x, 0.5) * step(cellDist.y, 0.5);
                    color = lerp(color, _MarkedColor, isInCell * _MarkedColor.a);
                }

                // view cone
                float2 localPos = (i.uv - 0.5) * _Zoom; // Centered around (0,0)
                float2 dirToPixel = normalize(localPos);
                float cosAngle = -dot(dirToPixel, normalize(_PlayerDirection)); // inverted for some reason
                float angleThreshold = cos(radians(_ViewConeAngle) * 0.5);
                float withinCone = step(angleThreshold, cosAngle) * step(length(localPos), _ViewConeLength);
                color = lerp(color, _ViewConeColor, withinCone * _ViewConeColor.a);

                // Player marker (always at center) with sharp edges
                float playerCircle = step(length(i.uv - 0.5), _PlayerSize);
                color = lerp(color, _PlayerColor, playerCircle * _PlayerColor.a);

                return color;
            }
            ENDCG
        }
    }
}
