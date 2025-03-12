using UnityEngine;

public class PingPongBuffer
{
    private RenderTexture[] textures = new RenderTexture[2];
    private int currentIndex = 0;

    public PingPongBuffer(int width, int height, RenderTextureFormat format)
    {
        for (int i = 0; i < 2; i++)
        {
            textures[i] = new RenderTexture(width, height, 0, format);
            textures[i].dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
            textures[i].enableRandomWrite = true;
            textures[i].volumeDepth = 6;
            textures[i].Create();
        }
    }

    public enum ClearMode
    {
        None,
        ColorBuffer,
        DepthBuffer
    }

    public RenderTexture[] Textures => textures;
    public RenderTexture Front => textures[currentIndex];
    public RenderTexture Back => textures[1 - currentIndex];

    public void Swap(ClearMode clearMode = ClearMode.None)
    {
        currentIndex = 1 - currentIndex;
        if (clearMode == ClearMode.None) return;

        // clear front texture
        RenderTexture rt = RenderTexture.active;
        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            Graphics.SetRenderTarget(Front, 0, CubemapFace.Unknown, faceIndex);

            switch (clearMode)
            {
                case ClearMode.ColorBuffer:
                    GL.Clear(true, true, Color.clear);
                    break;
                case ClearMode.DepthBuffer:
                    // a bit odd, but InterlockedMin needs a high value as a starting point to find smallest depth
                    GL.Clear(true, true, new Color(int.MaxValue, 0, 0));
                    break;
                default:
                    break;
            }
        }
        RenderTexture.active = rt;
    }
}
