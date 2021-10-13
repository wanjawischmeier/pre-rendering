using UnityEngine;

namespace PreRendering
{
    public class FrameBuffer : Buffer<Vector3, Texture2D>
    {
        public Texture2DArray textures;

        public FrameBuffer(int width, int height, int cacheSize, TextureFormat format = TextureFormat.RGBA32) : base(cacheSize)
        {
            textures = new Texture2DArray(width, height, cacheSize, format, 1, false);
        }

        ~FrameBuffer()
        {
            Object.Destroy(textures);
        }

        public override void Add(int index, Texture2D value)
        {
            Graphics.CopyTexture(textures, index, value, 0);
        }
    }
}