using UnityEngine;

public class MultiPass : MonoBehaviour
{
    public enum DebugChannel
    {
        none, motionVectors, projected, projectedDepth, rasterized, rasterizedDepth
    }

    public enum DebugMode
    {
        none, zSine, highlightPoint, highlighQuad, pointCloud, wireframe
    }

    const int MAX_PASSES = 4;

    public Texture2D input;
    public ComputeShader computeShader;
    public Shader postRasterization;
    [Range(1, MAX_PASSES)]
    public int passes = 1;
    public Vector2Int[] projectionResolutions;
    public Vector2Int rasterizationResolution;
    public float nClip, fClip;
    [Header("Only a temporary fix")]
    public bool fillGaps = true;

    [Header("Debugging")]
    public DebugChannel debugChannel = DebugChannel.rasterized;
    public DebugMode debugMode;
    public int debugInt;

    public RenderTexture motionVectors, projectedDepth, rasterized, rasterizedDepth;
    private RenderTexture[] projected;
    private Camera mainCamera;
    private Material postRasterizationMaterial;
    private int calculateMotionVectorGroupsX, calculateMotionVectorGroupsY, projectGroupsX, projectGroupsY, rasterizeGroupsX, rasterizeGroupsY;
    private int calculateMotionVectorsKernel, projectKernel, rasterizeKernel, interpolateKernel;

    private void Start()
    {
        mainCamera = Camera.main;
        postRasterizationMaterial = new Material(postRasterization);

        computeShader.EnableKeyword("PASS_0");
        calculateMotionVectorsKernel = computeShader.FindKernel("CalculateMotionVectors");
        projectKernel = computeShader.FindKernel("Project");
        rasterizeKernel = computeShader.FindKernel("Rasterize");
        interpolateKernel = computeShader.FindKernel("Interpolate");

        // calculate group sizes
        computeShader.GetKernelThreadGroupSizes(projectKernel, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        calculateMotionVectorGroupsX = input.width / (int)threadGroupSizeY;
        calculateMotionVectorGroupsY = input.height / (int)threadGroupSizeY;
        projectGroupsX = projectionResolutions[0].x / (int)threadGroupSizeY;
        projectGroupsY = projectionResolutions[0].y / (int)threadGroupSizeY;
        rasterizeGroupsX = rasterizationResolution.x / (int)threadGroupSizeY;
        rasterizeGroupsY = rasterizationResolution.y / (int)threadGroupSizeY;

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
        rasterized.format = RenderTextureFormat.RGFloat;
        rasterizedDepth = new RenderTexture(rasterized);
        rasterizedDepth.format = RenderTextureFormat.RFloat;

        // set compute shader constants
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", nClip);
        computeShader.SetFloat("FCLIP", fClip);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolutions[0].x, projectionResolutions[0].y));
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
        computeShader.SetTexture(interpolateKernel, "Rasterized", rasterized);

        for (int pass = 0; pass < passes; pass++)
        {
            computeShader.SetTexture(projectKernel, $"Projected{pass}", projected[pass]);
            computeShader.SetTexture(rasterizeKernel, $"Projected{pass}", projected[pass]);
        }

        // set post rasterization material properties
        postRasterizationMaterial.SetVector("RESOLUTION", new Vector2(rasterizationResolution.x, rasterizationResolution.y));
        postRasterizationMaterial.SetTexture("_Input", input);
        postRasterizationMaterial.SetTexture("_Coordinates", rasterized);
        
        // calculate motion vectors
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
        computeShader.SetFloat("CAM_NCLIP", mainCamera.nearClipPlane);
        computeShader.SetFloat("CAM_FCLIP", mainCamera.farClipPlane);
        computeShader.SetMatrix("MVP", MVP);

        // project and rasterize
        computeShader.Dispatch(projectKernel, projectGroupsX, projectGroupsY, 1);
        computeShader.Dispatch(rasterizeKernel, projectGroupsX, projectGroupsY, 1);
        
        if (fillGaps)
        {
            computeShader.Dispatch(interpolateKernel, rasterizeGroupsX, rasterizeGroupsY, 1);
        }
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
                Graphics.Blit(null, destination, postRasterizationMaterial);
                break;
        }
    }
}
