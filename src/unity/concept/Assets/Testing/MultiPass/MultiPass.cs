using UnityEngine;
using static UnityEditor.ShaderData;

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
    public Vector2Int[] projectionResolutions, rasterizationResolutions;
    public float nClip, fClip;
    [Header("Only a temporary fix")]
    public bool fillGaps = true;

    [Header("Debugging")]
    public DebugChannel debugChannel = DebugChannel.rasterized;
    public DebugMode debugMode;
    public int debugPass, debugInt;

    public RenderTexture motionVectors;
    private RenderTexture[] projected, projectedDepth, rasterized, rasterizedDepth;
    private Camera mainCamera;
    private Material postRasterizationMaterial;
    private int calculateMotionVectorGroupsX, calculateMotionVectorGroupsY;
    private int[] projectGroupsX, projectGroupsY, rasterizeGroupsX, rasterizeGroupsY;
    private int calculateMotionVectorsKernel, projectKernel, rasterizeKernel, interpolateKernel;

    private void Start()
    {
        mainCamera = Camera.main;
        postRasterizationMaterial = new Material(postRasterization);

        // disable all shader variants apart from the first one
        computeShader.EnableKeyword($"PASS_0");
        for (int pass = 1; pass < passes; pass++)
        {
            computeShader.DisableKeyword($"PASS_{pass}");
        }
        calculateMotionVectorsKernel = computeShader.FindKernel("CalculateMotionVectors");
        projectKernel = computeShader.FindKernel("Project");
        rasterizeKernel = computeShader.FindKernel("Rasterize");
        interpolateKernel = computeShader.FindKernel("Interpolate");

        // calculate group sizes
        computeShader.GetKernelThreadGroupSizes(projectKernel, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        calculateMotionVectorGroupsX = input.width / (int)threadGroupSizeY;
        calculateMotionVectorGroupsY = input.height / (int)threadGroupSizeY;

        // input dimensions
        motionVectors = new RenderTexture(input.width, input.height, 0);
        motionVectors.enableRandomWrite = true;
        motionVectors.format = RenderTextureFormat.ARGBFloat;

        // instantiate texture arrays
        projected = new RenderTexture[passes];
        projectedDepth = new RenderTexture[passes];
        rasterized = new RenderTexture[passes];
        rasterizedDepth = new RenderTexture[passes];
        projectGroupsX = new int[passes];
        projectGroupsY = new int[passes];
        rasterizeGroupsX = new int[passes];
        rasterizeGroupsY = new int[passes];

        for (int pass = 0; pass < passes; pass++)
        {
            // calculate the required thread group sizes for each pass in advance
            projectGroupsX[pass] = projectionResolutions[pass].x / (int)threadGroupSizeY;
            projectGroupsY[pass] = projectionResolutions[pass].y / (int)threadGroupSizeY;
            rasterizeGroupsX[pass] = rasterizationResolutions[pass].x / (int)threadGroupSizeY;
            rasterizeGroupsY[pass] = rasterizationResolutions[pass].y / (int)threadGroupSizeY;

            // projection dimensions
            projected[pass] = new RenderTexture(projectionResolutions[pass].x, projectionResolutions[pass].y, 0);
            projected[pass].enableRandomWrite = true;
            projected[pass].format = RenderTextureFormat.RGFloat;
            projectedDepth[pass] = new RenderTexture(projected[pass]);
            projectedDepth[pass].format = RenderTextureFormat.RFloat;

            // result/rasterization dimensions
            rasterized[pass] = new RenderTexture(rasterizationResolutions[pass].x, rasterizationResolutions[pass].y, 0);
            rasterized[pass].enableRandomWrite = true;
            rasterized[pass].filterMode = FilterMode.Bilinear;
            rasterized[pass].format = RenderTextureFormat.RGFloat;
            rasterizedDepth[pass] = new RenderTexture(rasterized[pass]);
            rasterizedDepth[pass].format = RenderTextureFormat.RFloat;

            // set compute shader texture array elements
            computeShader.SetTexture(projectKernel, $"Projected{pass}", projected[pass]);
            computeShader.SetTexture(projectKernel, $"ProjectedDepth{pass}", projectedDepth[pass]);
            computeShader.SetTexture(projectKernel, $"Rasterized{pass}", rasterized[pass]);
            computeShader.SetTexture(projectKernel, $"RasterizedDepth{pass}", rasterizedDepth[pass]);
            computeShader.SetTexture(rasterizeKernel, $"Projected{pass}", projected[pass]);
            computeShader.SetTexture(rasterizeKernel, $"ProjectedDepth{pass}", projectedDepth[pass]);
            computeShader.SetTexture(rasterizeKernel, $"Rasterized{pass}", rasterized[pass]);
            computeShader.SetTexture(rasterizeKernel, $"RasterizedDepth{pass}", rasterizedDepth[pass]);
            computeShader.SetTexture(interpolateKernel, $"Rasterized{pass}", rasterized[pass]);
        }

        // set compute shader constants
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", nClip);
        computeShader.SetFloat("FCLIP", fClip);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input.width, input.height));

        // set compute shader textures
        computeShader.SetTexture(calculateMotionVectorsKernel, "Input", input);
        computeShader.SetTexture(calculateMotionVectorsKernel, "MotionVectors", motionVectors);
        computeShader.SetTexture(projectKernel, "MotionVectors", motionVectors);

        // set post rasterization material properties
        postRasterizationMaterial.SetVector("RESOLUTION", new Vector2(rasterizationResolutions[passes - 1].x, rasterizationResolutions[passes - 1].y));
        postRasterizationMaterial.SetTexture("_Input", input);
        postRasterizationMaterial.SetTexture("_Coordinates", rasterized[passes - 1]);
        
        // calculate motion vectors
        computeShader.Dispatch(calculateMotionVectorsKernel, calculateMotionVectorGroupsX, calculateMotionVectorGroupsY, 1);
    }

    private void Update()
    {
        // calculate model-view-projection matrix (really just world-projection)
        Matrix4x4 MVP = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, true) * mainCamera.worldToCameraMatrix;

        // set compute shader values
        computeShader.SetInt("DEBUG_INT", debugInt);
        computeShader.SetInt("DEBUG_MODE", (int)debugMode);
        computeShader.SetFloat("TIMESTEP", Time.frameCount + Time.deltaTime);
        computeShader.SetFloat("CAM_NCLIP", mainCamera.nearClipPlane);
        computeShader.SetFloat("CAM_FCLIP", mainCamera.farClipPlane);
        computeShader.SetMatrix("MVP", MVP);

        // TODO: is it okay to change the active render texture for so long?
        RenderTexture rt = RenderTexture.active;

        for (int pass = 0; pass < passes; pass++)
        {
            // TODO: check which textures actually need to be cleared
            RenderTexture.active = projected[pass];
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = projectedDepth[pass];
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = rasterized[pass];
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = rasterizedDepth[pass];
            GL.Clear(false, true, Color.clear);

            // select the appropiate shader variant for the current pass
            if (pass != 0)
            {
                computeShader.DisableKeyword($"PASS_{pass - 1}");
                computeShader.SetVector("PREVIOUS_PROJECTION_RESOLUTION", new Vector2(projectionResolutions[pass - 1].x, projectionResolutions[pass - 1].y));
            }
            computeShader.EnableKeyword($"PASS_{pass}");
            computeShader.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolutions[pass].x, projectionResolutions[pass].y));
            computeShader.SetVector("RASTERIZATION_RESOLUTION", new Vector2(rasterizationResolutions[pass].x, rasterizationResolutions[pass].y));

            // project and rasterize
            computeShader.Dispatch(projectKernel, projectGroupsX[pass], projectGroupsY[pass], 1);
            computeShader.Dispatch(rasterizeKernel, projectGroupsX[pass], projectGroupsY[pass], 1);
            if (fillGaps)
            {
                computeShader.Dispatch(interpolateKernel, rasterizeGroupsX[pass], rasterizeGroupsY[pass], 1);
            }
        }

        RenderTexture.active = rt;

        computeShader.DisableKeyword($"PASS_{passes - 1}");
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        switch (debugChannel)
        {
            case DebugChannel.motionVectors:
                Graphics.Blit(motionVectors, destination);
                break;
            case DebugChannel.projected:
                Graphics.Blit(projected[debugPass], destination);
                break;
            case DebugChannel.projectedDepth:
                Graphics.Blit(projectedDepth[debugPass], destination);
                break;
            case DebugChannel.rasterized:
                Graphics.Blit(rasterized[debugPass], destination);
                break;
            case DebugChannel.rasterizedDepth:
                Graphics.Blit(rasterizedDepth[debugPass], destination);
                break;
            default:
                Graphics.Blit(null, destination, postRasterizationMaterial);
                break;
        }
    }
}
