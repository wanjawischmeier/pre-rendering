using PreRendering;
using System.IO;
using UnityEngine;

[System.Serializable]
public struct PackedPixel
{
    public uint Index, BG, RA;
    public ushort R, G, B, A;
    public Color color;
}

public class DecoderTest : MonoBehaviour
{
    public Shader shader;
    public string relativeImagePath;
    Material material;
    ComputeBuffer buffer;
    Texture2D texture;
    public Vector2Int res;
    public Vector4 pred;
    public int threads, idx;

    string imagePath;
    public uint[] data;
    public PackedPixel[] packedPixels;

    private void Start()
    {
        string rootPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
        imagePath = Path.Combine(rootPath, "pre-rendering/master/renders/", relativeImagePath);
        
        int size = res.x * res.y * threads * 2;
        buffer = new ComputeBuffer(size, sizeof(uint));
        
        material = new Material(shader);
        
        Vector2 texel = Vector2.one / (res - Vector2.one);
        Vector2 texelOffset = Vector2.one - texel + texel / 4;
        material.SetVector("Res", new Vector2(res.x, res.y));
        material.SetVector("TexelOffset", texelOffset);
        material.SetBuffer("Tex", buffer);
        
        Decoder.Initialize(imagePath, res.x, res.y);
        data = Decoder.buffer.ToArray();

        packedPixels = new PackedPixel[data.Length / 2];
        for (int i = 0; i < data.Length / 2; i++)
        {
            uint bg = data[i * 2];
            uint ra = data[i * 2 + 1];

            Unpack(bg, out ushort b, out ushort g);
            Unpack(ra, out ushort r, out ushort a);
            packedPixels[i] = new PackedPixel()
            {
                Index = (uint)i * 2,
                BG = bg,
                RA = ra,
                R = r,
                G = g,
                B = b,
                A = a,
                color = new Color(
                    r.Normalize(),
                    g.Normalize(),
                    b.Normalize(),
                    a.Normalize())
            };
        }
        
        texture = new Texture2D(res.x, res.y);
        texture.filterMode = FilterMode.Point;

        for (int x = 0; x < res.x; x++)
        {
            for (int y = 0; y < res.y; y++)
            {
                int idx = (x + (res.y - 1 - y) * res.x) * 2;

                uint bg = data[idx];
                uint ra = data[idx + 1];

                Unpack(bg, out ushort b, out ushort g);
                Unpack(ra, out ushort r, out ushort a);

                Color col = new Color(
                    r.Normalize(),
                    g.Normalize(),
                    b.Normalize(),
                    a.Normalize());

                texture.SetPixel(x, y, col);
            }
        }
        texture.Apply();
        
        // buffer.SetData();
        /*
        Decoder.ImageDecoded += Decoder_ImageDecoded;
        for (int i = 0; i < threads; i++)
        {
            Debug.Log($"Starting thread {i}");
            int j = i;

            Task.Run(() => Decoder.Decode(imagePath, ref data, token, j), token);
        }
        */
    }

    private void Update()
    {
        material.SetInt("Idx", idx);
        material.SetVector("Pred", pred);
        if (Decoder.buffer.IsCreated) buffer.SetData(Decoder.buffer);
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination) =>
        Graphics.Blit(source, destination, material);
    /*
    private void OnRenderImage(RenderTexture source, RenderTexture destination) =>
        Graphics.Blit(texture, destination);
    */
    private void OnDestroy()
    {
        Decoder.Deinitialize();
        buffer.Release();
    }

    private void Decoder_ImageDecoded(string path, long decodingTime)
    {
        Debug.Log(
            $"Decoded at {path}\n\n" +
            $"Decoded in {decodingTime}ms");
    }

    private void Unpack(uint v, out ushort v0, out ushort v1)
    {
        v0 = (ushort)(v & 0xFFFF);
        v1 = (ushort)(v >> 16);
    }
}
