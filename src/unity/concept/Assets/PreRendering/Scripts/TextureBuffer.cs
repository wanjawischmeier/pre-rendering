using UnityEngine;

namespace PreRendering
{
    public class TextureBuffer : Buffer<Vector3, Texture2D>
    {
        public Texture2DArray textures;

        public TextureBuffer(int width, int height, int cacheSize, TextureFormat format = TextureFormat.RGBA32) : base(cacheSize)
        {
            textures = new Texture2DArray(width, height, cacheSize, format, 1, false);
        }

        ~TextureBuffer() => Release();

        public void Release() =>
            Object.Destroy(textures);


        public override void Add(int index, Texture2D value)
        {
            if (textures == null) return;
            Graphics.CopyTexture(value, 0, textures, index);
        }
    }
}