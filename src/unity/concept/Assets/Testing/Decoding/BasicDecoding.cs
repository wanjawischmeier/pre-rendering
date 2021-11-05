using UnityEngine;
using PreRendering;
using System.IO;

public class BasicDecoding : MonoBehaviour
{
    public Shader shader;
    public string relativeImagePath;
    public Vector2Int resolution;

    Material material;
    ComputeBuffer buffer;
    string imagePath;

    private void Start()
    {
        string rootPath = Application.dataPath.Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
        imagePath = Path.Combine(rootPath, "pre-rendering/master/renders/", relativeImagePath);

        int size = resolution.x * resolution.y * 2;
        buffer = new ComputeBuffer(size, sizeof(uint));

        material = new Material(shader);

        material.SetBuffer("RawTexture", buffer);
        material.SetVector("Resolution", new Vector2(resolution.x, resolution.y));

        Decoder.Initialize(imagePath, resolution.x, resolution.y);
        Decoder.ImageDecoded += OnImageDecoded;

        Decoder.Decode(imagePath);

        buffer.SetData(Decoder.buffer);
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
