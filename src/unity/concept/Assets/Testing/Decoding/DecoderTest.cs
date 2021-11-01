using PreRendering;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using DecodingStats = PreRendering.Decoder.DecodingStats;

public class DecoderTest : MonoBehaviour
{
    public Shader shader;
    public string relativeImagePath;
    Material material;
    ComputeBuffer buffer;
    public Vector2Int res;
    public int threads;
    List<Task> tasks;
    CancellationTokenSource tokenSource;
    CancellationToken token;
    Thread mainThread;
    string imagePath;
    uint[] data;

    private void Start()
    {
        string rootPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
        imagePath = Path.Combine(rootPath, "pre-rendering/master/renders/", relativeImagePath);

        mainThread = Thread.CurrentThread;
        tokenSource = new CancellationTokenSource();
        token = tokenSource.Token;
        tasks = new List<Task>();
        
        int size = res.x * res.y * threads * 2;
        data = new uint[size];
        buffer = new ComputeBuffer(size, sizeof(uint));
        
        material = new Material(shader);
        material.SetVector("res", new Vector2(res.x, res.y));
        material.SetBuffer("Tex", buffer);

        Decoder.Initialize(imagePath, res.x, res.y);
        
        Decoder.ImageDecoded += Decoder_ImageDecoded;
        for (int i = 0; i < threads; i++)
        {
            Debug.Log(string.Format("Starting thread {0}", i));
            int j = i;

            Task task = Task.Run(() => Decoder.Decode(imagePath, ref data, token, j), token);
            tasks.Add(task);
        }
        
    }

    private void Update()
    {
        if (data != null) buffer.SetData(data);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) =>
        Graphics.Blit(source, destination, material);

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
