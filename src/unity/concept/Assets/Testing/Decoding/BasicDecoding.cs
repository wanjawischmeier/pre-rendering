using UnityEngine;
using PreRendering;
using System.IO;

public class BasicDecoding : MonoBehaviour
{
    public Shader shader;
    public string[] relativeImagePaths;
    public int selected;
    public Vector2Int resolution;

    Material material;
    ComputeBuffer buffer;
    int depth, size;

    const string repoName = "pre-rendering";

    private void Start()
    {
        depth = relativeImagePaths.Length;
        string rootPath = Application.dataPath.Split(new string[] { repoName }, System.StringSplitOptions.None)[0];
        string sampleImagePath = Path.Combine(rootPath, repoName, "renders", relativeImagePaths[0]);

        size = resolution.x * resolution.y * depth * 2;
        buffer = new ComputeBuffer(size, sizeof(uint));
        
        material = new Material(shader);

        material.SetBuffer("RawTexture", buffer);
        material.SetVector("Resolution", new Vector2(resolution.x, resolution.y));

        Decoder.Initialize(sampleImagePath, depth, resolution.x, resolution.y);
        Decoder.ImageDecoded += OnImageDecoded;

        for (int i = 0; i < depth; i++)
        {
            string imagePath = Path.Combine(rootPath, repoName, "renders", relativeImagePaths[i]);
            Decoder.Decode(imagePath, i);
        }

        buffer.SetData(Decoder.buffer);
    }

    private void Update()
    {
        if (selected < 0) selected = 0;
        if (selected >= depth) selected = depth -1;
        material.SetInt("Offset", resolution.x * resolution.y * selected);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) =>
        Graphics.Blit(null, destination, material);

    private void OnImageDecoded(string path, long decodingTime, int threadId)
    {
        string threadInfo = threadId == -1 ? "" : $"\t\t(ThreadID:{threadId})";
        Debug.Log($"Decoded {Path.GetFileName(path)} in {decodingTime}ms" + threadInfo);
    }

    private void OnDestroy()
    {
        Decoder.Deinitialize();
        buffer.Release();
    }
}
