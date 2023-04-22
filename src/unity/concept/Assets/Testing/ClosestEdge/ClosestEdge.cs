using UnityEngine;

public class ClosestEdge : MonoBehaviour
{
    public Texture2D input;
    public ComputeShader computeShader;
    public Vector2 lineRasterizerClipRange = new Vector2(0, 100);
    public float maxSearchIterations = 100;
    public bool debug;

    private RenderTexture transformLookup, transformed, depth, result;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int kernel, interpolate, threadGroupsX, threadGroupsY;

    private void Start()
    {
        kernel = computeShader.FindKernel("Project");
        interpolate = computeShader.FindKernel("Interpolate");
        computeShader.GetKernelThreadGroupSizes(kernel, out threadGroupSizeX, out threadGroupSizeY, out _);
        threadGroupsX = input.width / (int)threadGroupSizeX;
        threadGroupsY = input.height / (int)threadGroupSizeY;

        result = new RenderTexture(input.width, input.height, 0);
        result.enableRandomWrite = true;
        depth = new RenderTexture(result);
        depth.format = RenderTextureFormat.RFloat;
        transformLookup = new RenderTexture(result);
        transformLookup.format = RenderTextureFormat.RGFloat;
        transformed = new RenderTexture(transformLookup);

        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetTexture(kernel, "Input", input);
        computeShader.SetTexture(kernel, "TLookup", transformLookup);
        computeShader.SetTexture(kernel, "Transformed", transformed);
        computeShader.SetTexture(kernel, "Depth", depth);
        computeShader.SetTexture(kernel, "Result", result);
        computeShader.SetTexture(interpolate, "Input", input);
        computeShader.SetTexture(interpolate, "TLookup", transformLookup);
        computeShader.SetTexture(interpolate, "Transformed", transformed);
        computeShader.SetTexture(interpolate, "Depth", depth);
        computeShader.SetTexture(interpolate, "Result", result);
    }

    private void Update()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = transformLookup;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = transformed;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = depth;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;

        computeShader.SetBool("DEBUG", debug);
        computeShader.SetFloat("SEARCH_ITERATIONS", maxSearchIterations);
        computeShader.SetVector("LINE_CLIP", lineRasterizerClipRange);
        computeShader.SetVector("OFFSET", transform.position);
        computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
        computeShader.Dispatch(interpolate, threadGroupsX, threadGroupsY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
