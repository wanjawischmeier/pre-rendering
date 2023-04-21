using UnityEngine;

public class GridFiltering : MonoBehaviour
{
    public Texture2D input;
    public ComputeShader computeShader;
    public int tileSize;

    private RenderTexture result, depth;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int kernelTransform, kernelInterpolate, threadGroupsX, threadGroupsY;

    private void Start()
    {
        result = new RenderTexture(input.width, input.height, 0);
        result.enableRandomWrite = true;
        depth = new RenderTexture(result);
        depth.format = RenderTextureFormat.RFloat;

        kernelTransform = computeShader.FindKernel("Transform");
        kernelInterpolate = computeShader.FindKernel("Interpolate");
        computeShader.GetKernelThreadGroupSizes(kernelTransform, out threadGroupSizeX, out threadGroupSizeY, out _);
        threadGroupsX = input.width / (int)threadGroupSizeX;
        threadGroupsY = input.height / (int)threadGroupSizeY;

        computeShader.SetTexture(kernelTransform, "Input", input);
        computeShader.SetTexture(kernelTransform, "Result", result);
        computeShader.SetTexture(kernelTransform, "Depth", depth);
        computeShader.SetTexture(kernelInterpolate, "Result", result);
        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
    }

    private void Update()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = depth;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;

        computeShader.SetFloat("TILE_SIZE", input.width / Mathf.Pow(2, tileSize));
        computeShader.SetVector("OFFSET", transform.position);
        computeShader.Dispatch(kernelTransform, threadGroupsX, threadGroupsY, 1);
        computeShader.Dispatch(kernelInterpolate, threadGroupsX, threadGroupsY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
