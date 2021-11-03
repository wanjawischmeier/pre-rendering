using PreRendering;
using System.IO;
using System.Threading;
using UnityEngine;
using DecodingStats = PreRendering.Decoder.DecodingStats;

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

    CancellationTokenSource tokenSource;
    CancellationToken token;
    string imagePath;
    public uint[] data, reversed;
    public ushort[] unpacked;

    private void Start()
    {
        string rootPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
        imagePath = Path.Combine(rootPath, "pre-rendering/master/renders/", relativeImagePath);
        
        tokenSource = new CancellationTokenSource();
        token = tokenSource.Token;
        
        int size = res.x * res.y * threads * 2;
        buffer = new ComputeBuffer(size, sizeof(uint));
        
        material = new Material(shader);

        Vector2 texel = Vector2.one / (res - Vector2.one);
        Vector2 texelOffset = Vector2.one - texel + texel / 4;
        material.SetVector("res", new Vector2(res.x, res.y));
        material.SetVector("TexelOffset", texelOffset);
        material.SetBuffer("Tex", buffer);

        Decoder.Initialize(imagePath, res.x, res.y);
        data = Decoder.buffer.ToArray();

        reversed = new uint[data.Length];
        unpacked = new ushort[data.Length * 2];
        for (int i = 0; i < data.Length; i++)
        {
            Decoder.Unpack(data[i], out ushort v0, out ushort v1);
            unpacked[i * 2]     = (ushort)(v1 / (float)0xFFFF * 0xFF);
            unpacked[i * 2 + 1] = (ushort)(v0 / (float)0xFFFF * 0xFF);
            reversed[i] = Decoder.Pack(v1, v0);
        }


        texture = new Texture2D(res.x, res.y);
        texture.filterMode = FilterMode.Point;

        for (int x = 0; x < res.x; x++)
        {
            for (int y = 0; y < res.y; y++)
            {
                int idx = x + y * res.x * 2;

                uint raw0 = data[idx];
                uint raw1 = data[idx + 1];

                Decoder.Unpack(raw0, out ushort v0, out ushort v1);
                Decoder.Unpack(raw1, out ushort v2, out ushort v3);


                Color col;

                Vector3 uv = new Vector3(
                    x / (float)res.x,
                    y / (float)res.y);

                col = new Color(uv.x, uv.y, 0);
                // col = new Color(v0, v1, v2, v3);

                texture.SetPixel(x, y, col);
            }
        }
        texture.Apply();
        // buffer.SetData();
        /*
        Decoder.ImageDecoded += Decoder_ImageDecoded;
        for (int i = 0; i < threads; i++)
        {
            Debug.Log(string.Format("Starting thread {0}", i));
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
    /*
    private void OnRenderImage(RenderTexture source, RenderTexture destination) =>
        Graphics.Blit(source, destination, material);
    */
    private void OnRenderImage(RenderTexture source, RenderTexture destination) =>
        Graphics.Blit(texture, destination);

    private void OnDestroy()
    {
        tokenSource.Cancel();
        
        Decoder.Deinitialize();
        buffer.Release();
    }

    private void Decoder_ImageDecoded(string path, DecodingStats stats)
    {
        Debug.Log(string.Format(
            "Decoded at {0}\n\nStats:\n{1}",
            path, stats));
    }
}
