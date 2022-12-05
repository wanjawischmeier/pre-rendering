using UnityEngine;

public class SquareRasterization : MonoBehaviour
{
    public Texture2D input;
    public ComputeShader computeShader;
    [Range(1, 100)]
    public int range;
    [Range(1, 50)]
    public int falloff;

    public RenderTexture result, depthBuffer;
    private Vector3 lastPosition;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int transformKernel, mergeKernel, threadGroupsX, threadGroupsY;

    private void Start()
    {
        transformKernel = computeShader.FindKernel("Transform");
        mergeKernel = computeShader.FindKernel("Merge");
        computeShader.GetKernelThreadGroupSizes(transformKernel, out threadGroupSizeX, out threadGroupSizeY, out _);
        threadGroupsX = input.width / (int)threadGroupSizeX;
        threadGroupsY = input.height / (int)threadGroupSizeY;

        result = new RenderTexture(input.width, input.height, 0);
        result.enableRandomWrite = true;
        result.format = RenderTextureFormat.ARGBFloat;
        depthBuffer = new RenderTexture(result);
        depthBuffer.format = RenderTextureFormat.RFloat;

        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetTexture(transformKernel, "Input", input);
        computeShader.SetTexture(transformKernel, "Result", result);
        computeShader.SetTexture(transformKernel, "DepthBuffer", depthBuffer);
        computeShader.SetTexture(mergeKernel, "Input", input);
        computeShader.SetTexture(mergeKernel, "Result", result);
        computeShader.SetTexture(mergeKernel, "DepthBuffer", depthBuffer);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
    }

    private void Update()
    {
        if (false && transform.position == lastPosition)
            return;
        lastPosition = transform.position;

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;

        computeShader.SetVector("OFFSET", transform.position);
        computeShader.SetInt("RANGE", range);
        computeShader.SetInt("FALLOFF", falloff);
        computeShader.SetFloat("MAX_RANGE", new Vector2(range, range).magnitude);
        computeShader.Dispatch(transformKernel, threadGroupsX, threadGroupsY, 1);
        computeShader.Dispatch(mergeKernel, threadGroupsX, threadGroupsY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
