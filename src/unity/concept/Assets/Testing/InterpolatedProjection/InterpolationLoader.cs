using PreRendering;
using System.IO;
using UnityEngine;

public class InterpolationLoader : MonoBehaviour
{
    public string path;
    public int img, maxInterpolationIters = 10;
    public ComputeShader computeShader;
    public Shader shader;
    public MapConfig config;
    public Texture2DArray textures;
    public Vector3[] offsets;
    ComputeBuffer offsetBuffer;
    Texture2D sampleTexture;
    RenderTexture translated, result;
    Material material;
    Camera mainCamera;
    int translation, interpolation;

    private void Start()
    {
        sampleTexture = new Texture2D(0, 0, TextureFormat.RGB24, false);

        string[] files = Directory.GetFiles(path, "*.png");
        string sampleFile = Path.Combine(path, files[0]);
        byte[] data = File.ReadAllBytes(sampleFile);
        sampleTexture.LoadImage(data);

        textures = new Texture2DArray(sampleTexture.width, sampleTexture.height, files.Length, sampleTexture.format, false);
        translated = new RenderTexture(sampleTexture.width, sampleTexture.height, 16, RenderTextureFormat.ARGB64);
        translated.enableRandomWrite = true;
        result = new RenderTexture(translated);
        offsetBuffer = new ComputeBuffer(offsets.Length, sizeof(float) * 3);
        offsetBuffer.SetData(offsets);

        for (int i = 0; i < files.Length; i++)
        {
            string file = Path.Combine(path, files[i]);
            data = File.ReadAllBytes(file);
            sampleTexture.LoadImage(data);
            Graphics.CopyTexture(sampleTexture, 0, textures, i);
        }

        translation = computeShader.FindKernel("Translation");
        interpolation = computeShader.FindKernel("Interpolation");
        computeShader.SetTexture(translation, "InputBuffer", textures);
        computeShader.SetTexture(translation, "Translated", translated);
        computeShader.SetTexture(interpolation, "InputTranslated", translated);
        computeShader.SetTexture(interpolation, "Result", result);
        computeShader.SetBuffer(translation, "OffsetBuffer", offsetBuffer);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(sampleTexture.width, sampleTexture.height));
        computeShader.SetInt("IMG_IDX", 0);
        computeShader.SetInt("MAX_INTERP_ITERS", maxInterpolationIters);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", config.nclip);
        computeShader.SetFloat("FCLIP", config.fclip);
        computeShader.SetFloats("MULTIPLIERS", new float[]
        {
            0, -1,  // UP
            0, 1,   // DOWN
            -1, 0,  // LEFT
            1, 0    // RIGHT
        });

        material = new Material(shader);
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
        material.SetVector("POSITION_OFFSET", Vector3.zero);

        mainCamera = Camera.main;
    }

    private void Update()
    {
        computeShader.SetInt("IMG_IDX", img);
        computeShader.SetVector("POSITION", transform.position);
        computeShader.SetVector("ROTATION", transform.eulerAngles * Mathf.Deg2Rad);

        material.SetFloat("FOV", mainCamera.fieldOfView * Mathf.Deg2Rad);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        computeShader.Dispatch(translation, sampleTexture.width, sampleTexture.height, 1);
        computeShader.Dispatch(interpolation, sampleTexture.width, sampleTexture.height, 1);
        Graphics.Blit(result, destination);

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = translated;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;
    }

    private void OnDestroy()
    {
        offsetBuffer.Release();
    }
}
