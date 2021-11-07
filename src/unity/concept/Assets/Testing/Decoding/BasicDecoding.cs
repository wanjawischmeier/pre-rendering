using UnityEngine;
using PreRendering;
using System.IO;
using System.Threading;

public class BasicDecoding : MonoBehaviour
{
    public Shader shader;
    public string[] relativeImagePaths;
    public int selected;
    public Vector2Int resolution;

    Material material;
    RawTexture.Buffer buffer;
    DecodingThread decoder;
    int depth;

    const string repoName = "pre-rendering";

    private void Start()
    {
        depth = relativeImagePaths.Length;
        string rootPath = Application.dataPath.Split(new string[] { repoName }, System.StringSplitOptions.None)[0];
        string sampleImagePath = Path.Combine(rootPath, repoName, "renders", relativeImagePaths[0]);

        Decoder.Initialize(sampleImagePath, depth, resolution.x, resolution.y);

        buffer = new RawTexture.Buffer(Decoder.bufferPointer, resolution.x, resolution.y, depth);
        decoder = new DecodingThread(buffer, depth, depth);
        
        material = new Material(shader);
        material.SetBuffer("RawTexture", buffer.computeBuffer);
        material.SetVector("Resolution", new Vector2(resolution.x, resolution.y));

        for (int i = 0; i < depth; i++)
        {
            string imagePath = Path.Combine(rootPath, repoName, "renders", relativeImagePaths[i]);
            Debug.Log($"Starting thread {i}");
            decoder.DecodeToBufferAsync(imagePath, Vector3.zero);
        }
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

    private void OnDestroy()
    {
        decoder.Release();
        buffer.Release();
    }
}
