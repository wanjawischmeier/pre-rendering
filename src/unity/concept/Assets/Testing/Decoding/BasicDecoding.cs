using PreRendering;
using System.IO;
using UnityEngine;

public class BasicDecoding : MonoBehaviour
{
    public Shader shader;
    public string[] relativeImagePaths;
    public int selected;
    public Vector2Int resolution;
    private Material material;
    private DecodingBuffer buffer;
    private DecodingManager decoder;
    private int depth;
    private const string RepoName = "pre-rendering";

    private void Start()
    {
        depth = relativeImagePaths.Length;
        string rootPath = Application.dataPath.Split(new string[] { RepoName }, System.StringSplitOptions.None)[0];
        string sampleImagePath = Path.Combine(rootPath, RepoName, "images", relativeImagePaths[0]);

        // DecoderOld.Initialize(sampleImagePath, depth, resolution.x, resolution.y);
        // buffer = new RawTexture.NativeBuffer(Decoder.bufferPointer, resolution.x, resolution.y, depth, RawTexture.Format.RGBA64);
        decoder = new DecodingManager(sampleImagePath, 1);

        material = new Material(shader);
        material.SetBuffer("RawTexture", buffer.compute);
        material.SetVector("Resolution", new Vector2(resolution.x, resolution.y));

        for (int i = 0; i < depth; i++)
        {
            string imagePath = Path.Combine(rootPath, RepoName, "renders", relativeImagePaths[i]);
            Debug.Log($"Request {i} with path {imagePath}");
            // decoder.DecodeToBufferAsync(imagePath, new Vector3(0, 0, i));
        }
    }

    private void Update()
    {
        buffer.Refresh();

        if (selected < 0) selected = 0;
        if (selected >= depth) selected = depth - 1;
        material.SetInt("TextureOffset", resolution.x * resolution.y * selected);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) =>
        Graphics.Blit(null, destination, material);

    private void OnDestroy()
    {
        decoder.Release();
        buffer.Release();
    }
}
