using UnityEditor.Build;
using UnityEngine;

public class MultiPass : MonoBehaviour
{
    public enum DebugChannel
    {
        none, motionVectors, projected, projectedDepth, rasterized, rasterizedDepth
    }

    public enum DebugMode
    {
        none, zSine, highlightPoint, highlighVertex
    }

    const int MAX_PASSES = 4;

    public Texture2D input;
    public ComputeShader computeShader;
    public Vector2Int projectionResolution, rasterizationResolution;
    public Vector2Int[] projectionResolutions;
    [Range(1, MAX_PASSES)]
    public int passes = 1;
    public int debugInt;
    public DebugChannel debugChannel = DebugChannel.rasterized;
    public DebugMode debugMode;

    private RenderTexture motionVectors, projectedDepth, rasterized, rasterizedDepth;
    private RenderTexture[] projected;
    private Camera mainCamera;
    private int calculateMotionVectorGroupsX, calculateMotionVectorGroupsY, projectGroupsX, projectGroupsY;
    private int calculateMotionVectorsKernel, projectKernel, rasterizeKernel;

    private void Start()
    {
        mainCamera = Camera.main;

        computeShader.EnableKeyword("PASS_0");
        calculateMotionVectorsKernel = computeShader.FindKernel("CalculateMotionVectors");
        projectKernel = computeShader.FindKernel("Project");
        rasterizeKernel = computeShader.FindKernel("Rasterize");

        // calculate group sizes
        computeShader.GetKernelThreadGroupSizes(projectKernel, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        calculateMotionVectorGroupsX = input.width / (int)threadGroupSizeY;
        calculateMotionVectorGroupsY = input.height / (int)threadGroupSizeY;
        projectGroupsX = projectionResolution.x / (int)threadGroupSizeY;
        projectGroupsY = projectionResolution.y / (int)threadGroupSizeY;

        // input dimensions
        motionVectors = new RenderTexture(input.width, input.height, 0);
        motionVectors.enableRandomWrite = true;
        motionVectors.format = RenderTextureFormat.ARGBFloat;

        // projection dimensions
        projected = new RenderTexture[passes];
        for (int i = 0; i < passes; i++)
        {
            projected[i] = new RenderTexture(projectionResolutions[i].x, projectionResolutions[i].y, 0);
            projected[i].enableRandomWrite = true;
            projected[i].format = RenderTextureFormat.RGFloat;
        }

        projectedDepth = new RenderTexture(projected[0]);
        projectedDepth.format = RenderTextureFormat.RFloat;


        // result/rasterization dimensions
        rasterized = new RenderTexture(rasterizationResolution.x, rasterizationResolution.y, 0);
        rasterized.enableRandomWrite = true;
        rasterized.filterMode = FilterMode.Bilinear;
        rasterizedDepth = new RenderTexture(rasterized);
        rasterizedDepth.format = RenderTextureFormat.RFloat;

        // set compute shader constants
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolution.x, projectionResolution.y));
        computeShader.SetVector("RASTERIZATION_RESOLUTION", new Vector2(rasterizationResolution.x, rasterizationResolution.y));

        // set compute shader textures
        computeShader.SetTexture(calculateMotionVectorsKernel, "Input", input);
        computeShader.SetTexture(calculateMotionVectorsKernel, "MotionVectors", motionVectors);
        computeShader.SetTexture(projectKernel, "Input", input);
        computeShader.SetTexture(projectKernel, "MotionVectors", motionVectors);
        // computeShader.SetTexture(projectKernel, $"Projected{pass}", projected);
        computeShader.SetTexture(projectKernel, "ProjectedDepth", projectedDepth);
        computeShader.SetTexture(rasterizeKernel, "Input", input);
        // computeShader.SetTexture(rasterizeKernel, $"Projected{pass}", projected);
        computeShader.SetTexture(rasterizeKernel, "ProjectedDepth", projectedDepth);
        computeShader.SetTexture(rasterizeKernel, "Rasterized", rasterized);
        computeShader.SetTexture(rasterizeKernel, "RasterizedDepth", rasterizedDepth);

        for (int pass = 0; pass < passes; pass++)
        {
            computeShader.SetTexture(projectKernel, $"Projected{pass}", projected[pass]);
            computeShader.SetTexture(rasterizeKernel, $"Projected{pass}", projected[pass]);
        }
        
        // calculate 
        computeShader.Dispatch(calculateMotionVectorsKernel, calculateMotionVectorGroupsX, calculateMotionVectorGroupsY, 1);
    }

    private void Update()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = projectedDepth;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rasterized;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rasterizedDepth;
        GL.Clear(false, true, Color.clear);

        for (int pass = 0; pass < passes; pass++)
        {
            RenderTexture.active = projected[pass];
            GL.Clear(false, true, Color.clear);
        }

        RenderTexture.active = rt;

        // calculate model-view-projection matrix (really just world-projection)
        Matrix4x4 MVP = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, true) * mainCamera.worldToCameraMatrix;

        // set compute shader values
        computeShader.SetInt("DEBUG_INT", debugInt);
        computeShader.SetInt("DEBUG_MODE", (int)debugMode);
        computeShader.SetFloat("TIMESTEP", Time.frameCount + Time.deltaTime);
        computeShader.SetMatrix("MVP", MVP);

        // project and rasterize
        computeShader.Dispatch(projectKernel, projectGroupsX, projectGroupsY, 1);
        computeShader.Dispatch(rasterizeKernel, projectGroupsX, projectGroupsY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        switch (debugChannel)
        {
            case DebugChannel.motionVectors:
                Graphics.Blit(motionVectors, destination);
                break;
            case DebugChannel.projected:
                Graphics.Blit(projected[passes -1], destination);
                break;
            case DebugChannel.projectedDepth:
                Graphics.Blit(projectedDepth, destination);
                break;
            case DebugChannel.rasterized:
                Graphics.Blit(rasterized, destination);
                break;
            case DebugChannel.rasterizedDepth:
                Graphics.Blit(rasterizedDepth, destination);
                break;
            default:
                Graphics.Blit(source, destination);
                break;
        }
    }
}
