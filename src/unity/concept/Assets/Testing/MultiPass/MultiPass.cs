using UnityEngine;

public class MultiPass : MonoBehaviour
{
    public enum DebugChannel
    {
        none, projected, projectedDepth, rasterized, rasterizedDepth
    }

    public enum DebugMode
    {
        none, zSine, highlightPoint
    }

    public Texture2D input;
    public ComputeShader computeShader;
    public Vector2Int projectionResolution, rasterizationResolution;
    public int debugInt;
    public bool debug = false;
    public DebugChannel debugChannel = DebugChannel.rasterized;
    public DebugMode debugMode;

    private RenderTexture motionVectors, projected, projectedDepth, rasterized, rasterizedDepth;
    private Camera mainCamera;
    private int calculateMotionVectorGroupsX, calculateMotionVectorGroupsY, projectGroupsX, projectGroupsY, rasterizeGroupsX, rasterizeGroupsY;
    private int calculateMotionVectorsKernel, projectKernel, rasterizeKernel;
    private bool previousDebug = false;

    private void Start()
    {
        mainCamera = Camera.main;

        calculateMotionVectorsKernel = computeShader.FindKernel("CalculateMotionVectors");
        projectKernel = computeShader.FindKernel("Project");
        rasterizeKernel = computeShader.FindKernel("Rasterize");

        computeShader.GetKernelThreadGroupSizes(projectKernel, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        calculateMotionVectorGroupsX = input.width / (int)threadGroupSizeY;
        calculateMotionVectorGroupsY = input.height / (int)threadGroupSizeY;
        projectGroupsX = projectionResolution.x / (int)threadGroupSizeY;
        projectGroupsY = projectionResolution.y / (int)threadGroupSizeY;
        rasterizeGroupsX = rasterizationResolution.x / (int)threadGroupSizeY;
        rasterizeGroupsY = rasterizationResolution.y / (int)threadGroupSizeY;

        // input dimensions
        motionVectors = new RenderTexture(input.width, input.height, 0);
        motionVectors.enableRandomWrite = true;
        motionVectors.format = RenderTextureFormat.ARGBFloat;

        // projection dimensions
        projected = new RenderTexture(projectionResolution.x, projectionResolution.y, 0);
        projected.enableRandomWrite = true;
        projected.format = RenderTextureFormat.RGFloat;
        projectedDepth = new RenderTexture(projected);
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
        computeShader.SetTexture(projectKernel, "Projected", projected);
        computeShader.SetTexture(projectKernel, "ProjectedDepth", projectedDepth);
        computeShader.SetTexture(rasterizeKernel, "Input", input);
        computeShader.SetTexture(rasterizeKernel, "Projected", projected);
        computeShader.SetTexture(rasterizeKernel, "ProjectedDepth", projectedDepth);
        computeShader.SetTexture(rasterizeKernel, "Rasterized", rasterized);
        computeShader.SetTexture(rasterizeKernel, "RasterizedDepth", rasterizedDepth);
        
        // calculate 
        computeShader.Dispatch(calculateMotionVectorsKernel, calculateMotionVectorGroupsX, calculateMotionVectorGroupsY, 1);
    }

    private void Update()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = projected;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = projectedDepth;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rasterized;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rasterizedDepth;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;
        
        if (debug != previousDebug)
        {
            if (debug)
            {
                computeShader.EnableKeyword("WIREFRAME");
            }
            else
            {
                computeShader.DisableKeyword("WIREFRAME");
            }

            previousDebug = debug;
        }

        // calculate model-view-projection matrix (really just world-projection)
        Matrix4x4 MVP = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, true) * mainCamera.worldToCameraMatrix;

        // set compute shader values
        computeShader.SetInt("DEBUG_MODE", (int)debugMode);
        computeShader.SetFloat("TIMESTEP", Time.frameCount + Time.deltaTime);
        computeShader.SetMatrix("MVP", MVP);

        // project and rasterize
        computeShader.Dispatch(projectKernel, projectGroupsX, projectGroupsY, 1);
        computeShader.Dispatch(rasterizeKernel, rasterizeGroupsX, rasterizeGroupsY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        switch (debugChannel)
        {
            case DebugChannel.projected:
                Graphics.Blit(projected, destination);
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
