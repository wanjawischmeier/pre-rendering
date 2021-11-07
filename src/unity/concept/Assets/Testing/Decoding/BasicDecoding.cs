using UnityEngine;
using PreRendering;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

public class BasicDecoding : MonoBehaviour
{
    public Shader shader;
    public string[] relativeImagePaths;
    public int selected;
    public Vector2Int resolution;

    Material material;
    RawTexture.Buffer buffer;
    CancellationTokenSource tokenSource;
    CancellationToken token;
    int depth;
    bool canceled = false;

    const string repoName = "pre-rendering";

    private void Start()
    {
        depth = relativeImagePaths.Length;
        string rootPath = Application.dataPath.Split(new string[] { repoName }, System.StringSplitOptions.None)[0];
        string sampleImagePath = Path.Combine(rootPath, repoName, "renders", relativeImagePaths[0]);

        Decoder.Initialize(sampleImagePath, depth, resolution.x, resolution.y);
        Decoder.ImageDecoded += OnImageDecoded;

        buffer = new RawTexture.Buffer(Decoder.bufferPointer, resolution.x, resolution.y, depth);

        material = new Material(shader);
        material.SetBuffer("RawTexture", buffer.computeBuffer);
        material.SetVector("Resolution", new Vector2(resolution.x, resolution.y));

        string imagePath = Path.Combine(rootPath, repoName, "renders", relativeImagePaths[0]);
        tokenSource = new CancellationTokenSource();
        token = tokenSource.Token;
        Task.Run(() =>
        {
            Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Lowest;
            Decoder.Decode(imagePath, 0);
        });
    }

    private void Update()
    {
        buffer.Refresh();

        if (selected < 0) selected = 0;
        if (selected >= depth) selected = depth -1;
        material.SetInt("Offset", resolution.x * resolution.y * selected);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) =>
        Graphics.Blit(null, destination, material);

    private void OnImageDecoded(string path, int index, int threadId, long decodingTime)
    {
        buffer.Add(index);

        string threadInfo = threadId == -1 ? "" : $"\t\t(ThreadID:{threadId})";
        Debug.Log($"Decoded {Path.GetFileName(path)} in {decodingTime}ms" + threadInfo);

        if (token.IsCancellationRequested)
        {
            canceled = true;
            return;
        }

        Thread.Sleep((int)decodingTime * 2);
        Decoder.Decode(path, 0);
    }

    private void OnDestroy()
    {
        tokenSource.Cancel();
        while (!canceled) Thread.Sleep(10);

        Decoder.Deinitialize();
        buffer.Release();
    }
}
