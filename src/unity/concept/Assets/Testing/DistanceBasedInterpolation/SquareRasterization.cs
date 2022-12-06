using UnityEngine;

public class SquareRasterization : MonoBehaviour
{
    public Texture2D input1, input2;
    public ComputeShader computeShader;
    public Vector3 offset;
    [Range(1, 100)]
    public int range;
    [Range(1, 50)]
    public int falloff;
    [Range(1, 200)]
    public int max;
    [Range(1, 20)]
    public int off;

    public RenderTexture result, depthBuffer;
    private Vector3 lastPosition;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int transformKernel, mergeKernel, threadGroupsX, threadGroupsY;

    private void Start()
    {
        transformKernel = computeShader.FindKernel("Transform");
        mergeKernel = computeShader.FindKernel("Merge");
        computeShader.GetKernelThreadGroupSizes(transformKernel, out threadGroupSizeX, out threadGroupSizeY, out _);
        threadGroupsX = input1.width / (int)threadGroupSizeX;
        threadGroupsY = input1.height / (int)threadGroupSizeY;

        result = new RenderTexture(input1.width, input1.height, 0);
        result.enableRandomWrite = true;
        result.format = RenderTextureFormat.ARGBFloat;
        depthBuffer = new RenderTexture(result);
        depthBuffer.format = RenderTextureFormat.RFloat;

        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("MIN_FLOAT", float.MinValue);
        computeShader.SetVector("RESOLUTION", new Vector2(input1.width, input1.height));
        computeShader.SetTexture(transformKernel, "Result", result);
        computeShader.SetTexture(transformKernel, "DepthBuffer", depthBuffer);
        computeShader.SetTexture(mergeKernel, "Result", result);
        computeShader.SetTexture(mergeKernel, "DepthBuffer", depthBuffer);
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

        computeShader.SetInt("RANGE", range);
        computeShader.SetInt("FALLOFF", falloff);
        computeShader.SetInt("MAX", max);
        computeShader.SetInt("OFF", off);
        computeShader.SetFloat("MAX_RANGE", new Vector2(range, range).magnitude);
        computeShader.SetVector("OFFSET", transform.position);
        computeShader.SetTexture(transformKernel, "Input", input1);
        computeShader.Dispatch(transformKernel, threadGroupsX, threadGroupsY, 1);
        computeShader.SetVector("OFFSET", transform.position + offset);
        computeShader.SetTexture(transformKernel, "Input", input2);
        computeShader.Dispatch(transformKernel, threadGroupsX, threadGroupsY, 1);
        computeShader.Dispatch(mergeKernel, threadGroupsX, threadGroupsY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
