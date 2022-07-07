using UnityEngine;

public class MultipleTris : MonoBehaviour
{
    public ComputeShader computeShader;
    public Vector2Int resolution;
    public int intervall, distance;
    public bool wireframe;


    private RenderTexture result;
    private int rasterizationKernel;
    private uint threadGroupsX, threadGroupsY;

    private void Start()
    {
        result = new RenderTexture(100, 100, 1);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point;

        rasterizationKernel = computeShader.FindKernel("RasterizeTris");
        computeShader.GetKernelThreadGroupSizes(rasterizationKernel, out threadGroupsX, out threadGroupsY, out _);
        computeShader.SetTexture(rasterizationKernel, "Result", result);
        computeShader.SetVector("RESOLUTION", (Vector2)resolution);
    }

    private void Update()
    {
        if (wireframe) computeShader.EnableKeyword("WIREFRAME");
        else computeShader.DisableKeyword("WIREFRAME");

        computeShader.SetInt("INTERVALL", intervall);
        computeShader.SetInt("DISTANCE", distance);

        computeShader.Dispatch(rasterizationKernel, (int)(result.width / threadGroupsX / intervall) + 1, (int)(result.height / threadGroupsY / intervall) + 1, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
