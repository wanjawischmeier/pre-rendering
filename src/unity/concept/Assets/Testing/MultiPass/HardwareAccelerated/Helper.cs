using UnityEngine;

namespace PreRendering
{
    public static class Helper
    {
        public static RenderTexture GetTexture(this DynamicRenderBuffer[] renderBuffers, int pass = int.MaxValue, int slice = 0) =>
            renderBuffers[Mathf.Max(0, Mathf.Min(renderBuffers.Length - 1, pass))].targetTextures[slice];

        public static Vector2 ToVector2(this Resolution resolution) =>
            new Vector2(resolution.width, resolution.height);

        public static Vector2 ToVector2(this Vector2Int resolution) =>
            new Vector2(resolution.x, resolution.y);
    }
}