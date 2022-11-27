using UnityEngine;

public class DistanceBasedInterpolation : MonoBehaviour
{
    public Texture2D input;
    public ComputeShader computeShader;
    [Range(0, 100)]
    public int range;
    public float scale;

    public RenderTexture result, quadrants, depthBuffer;
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
        quadrants = new RenderTexture(result);
        quadrants.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        quadrants.volumeDepth = 4;
        depthBuffer = new RenderTexture(quadrants);
        depthBuffer.format = RenderTextureFormat.R16;

        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetTexture(transformKernel, "Input", input);
        computeShader.SetTexture(transformKernel, "Quadrants", quadrants);
        computeShader.SetTexture(transformKernel, "Result", result);
        computeShader.SetTexture(transformKernel, "DepthBuffer", depthBuffer);
        computeShader.SetTexture(mergeKernel, "Input", input);
        computeShader.SetTexture(mergeKernel, "Quadrants", quadrants);
        computeShader.SetTexture(mergeKernel, "Result", result);
        computeShader.SetTexture(mergeKernel, "DepthBuffer", depthBuffer);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
    }

    private void Update()
    {
        if (transform.position == lastPosition)
            return;
        lastPosition = transform.position;
        
        computeShader.SetVector("OFFSET", transform.position);
        computeShader.SetInt("RANGE", range);
        computeShader.SetFloat("MAX_RANGE", new Vector2(range, range).magnitude);
        computeShader.SetFloat("SCALE", scale);
        computeShader.Dispatch(transformKernel, threadGroupsX, threadGroupsY, 1);
        computeShader.Dispatch(mergeKernel, threadGroupsX, threadGroupsY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
