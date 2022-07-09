using PreRendering;
using System.IO;
using UnityEngine;

public class RasterizedProjection : MonoBehaviour
{
    // RasterizedInverseProjection (RIP, haha)

    public string path;
    public MapConfig config;
    public ComputeShader computeShader;
    public bool wireframe;

    private Texture2D input;
    private RenderTexture result;
    private int translationKernel, rasterizationKernel;
    private uint threadGroupsX, threadGroupsY;

    private void Start()
    {
        byte[] rawInput = File.ReadAllBytes(path);
        input = new Texture2D(0, 0, TextureFormat.RGBA64, false);
        input.LoadImage(rawInput);
        result = new RenderTexture(input.width, input.height, 1);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point; // JUST FOR TESTING!!!

        translationKernel = computeShader.FindKernel("Translate");
        rasterizationKernel = computeShader.FindKernel("Rasterize");
        
        computeShader.GetKernelThreadGroupSizes(rasterizationKernel, out threadGroupsX, out threadGroupsY, out _);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", config.nclip);
        computeShader.SetFloat("FCLIP", config.fclip);
        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetTexture(translationKernel, "Input", input);
        computeShader.SetTexture(translationKernel, "Result", result);
        
    }

    private void Update()
    {
        if (wireframe) computeShader.EnableKeyword("WIREFRAME");
        else computeShader.DisableKeyword("WIREFRAME");

        computeShader.SetVector("POSITION", transform.position);
        computeShader.Dispatch(translationKernel, (int)(input.width / threadGroupsX) + 1, (int)(input.height / threadGroupsY) + 1, 1);
        // computeShader.Dispatch(rasterizationKernel, (int)(input.width / threadGroupsX) + 1, (int)(input.height / threadGroupsY) + 1, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = rt;
    }
}
