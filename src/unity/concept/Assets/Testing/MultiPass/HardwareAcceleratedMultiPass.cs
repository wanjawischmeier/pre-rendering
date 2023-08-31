using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HardwareAcceleratedMultiPass : MonoBehaviour
{
    const int MAX_PASSES = 4;

    public enum DebugChannel
    {
        none, motionVectors, rasterized
    }

    public enum DebugMode
    {
        none, zSineFilled
    }

    public Texture2D input;
    public ComputeShader computeShader;
    public Shader rasterizationShader, postRasterizationShader;
    public float nClip, fClip, maxCircumference;
    public float fClipCutoff = 1;
    [Range(1, MAX_PASSES)]
    public int passes = 1;
    public Vector2Int projectionResolution, rasterizationResolution;
    public Vector3 meshTranslation;
    public AnimationCurve projectionResolutionCurve, rasterizationResolutionCurve;

    [Header("Debugging")]
    public DebugChannel debugChannel;
    public DebugMode debugMode;
    public int debugPass;

    [Header("Debugging Values")]
    public Camera[] renderCameras;
    public RenderTexture[] rasterized;
    public Vector2Int[] projectionResolutions, rasterizationResolutions;

    private Material rasterizationMaterial, postRasterizationMaterial;
    private RenderParams renderParams;
    private GraphicsBuffer[] meshTriangles, meshPositions, meshUVs;
    private RenderTexture motionVectors;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int calculateMotionVectorsKernel, loadTexelsToQuadBufferKernel;
    private int[] verticies, indicies;

    void Start()
    {
        // input dimensions
        motionVectors = new RenderTexture(input.width, input.height, 0);
        motionVectors.enableRandomWrite = true;
        motionVectors.format = RenderTextureFormat.ARGBFloat;

        // initialize arrays
        projectionResolutions = new Vector2Int[passes];
        rasterizationResolutions = new Vector2Int[passes];
        verticies = new int[passes];
        indicies = new int[passes];
        meshTriangles = new GraphicsBuffer[passes];
        meshPositions = new GraphicsBuffer[passes];
        meshUVs = new GraphicsBuffer[passes];
        rasterized = new RenderTexture[passes];
        renderCameras = new Camera[passes];
        /*
        // disable all shader variants apart from the first one
        computeShader.EnableKeyword($"PASS_0");
        for (int pass = 1; pass < passes; pass++)
        {
            computeShader.DisableKeyword($"PASS_{pass}");
        }
        */

        Camera originalCamera = GetComponent<Camera>();
        
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", nClip);
        computeShader.SetFloat("FCLIP", fClip);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input.width, input.height));

        calculateMotionVectorsKernel = computeShader.FindKernel("CalculateMotionVectors");
        loadTexelsToQuadBufferKernel = computeShader.FindKernel("LoadTexelsToQuadBuffer");
        computeShader.GetKernelThreadGroupSizes(loadTexelsToQuadBufferKernel, out threadGroupSizeX, out threadGroupSizeY, out _);

        computeShader.SetTexture(calculateMotionVectorsKernel, "_Input", input);
        computeShader.SetTexture(calculateMotionVectorsKernel, "_MotionVectorsWrite", motionVectors);
        computeShader.SetTexture(loadTexelsToQuadBufferKernel, "_MotionVectors", motionVectors);
        computeShader.SetTexture(loadTexelsToQuadBufferKernel, "_Input", input);

        // calculate motion vectors
        computeShader.Dispatch(calculateMotionVectorsKernel, input.width / (int)threadGroupSizeX, input.height / (int)threadGroupSizeY, 1);

        Vector2 tmpProjectionResolution = new Vector2(projectionResolution.x, projectionResolution.y);
        Vector2 tmpRasterizationResolution = new Vector2(rasterizationResolution.x, rasterizationResolution.y);

        for (int pass = 0; pass < passes; pass++)
        {
            float relativePass = (float)pass / (passes - 1);
            float relativeCurveMultiplier = projectionResolutionCurve.Evaluate(relativePass);
            projectionResolutions[pass] = Vector2Int.RoundToInt(tmpProjectionResolution * relativeCurveMultiplier);
            relativeCurveMultiplier = rasterizationResolutionCurve.Evaluate(relativePass);
            rasterizationResolutions[pass] = Vector2Int.RoundToInt(tmpRasterizationResolution * relativeCurveMultiplier);

            verticies[pass] = projectionResolutions[pass].x * projectionResolutions[pass].y;
            indicies[pass] = verticies[pass] * 6;

            meshTriangles[pass] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indicies[pass], sizeof(int));
            meshPositions[pass] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies[pass], 3 * sizeof(float));
            meshUVs[pass] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies[pass], 2 * sizeof(float));

            rasterized[pass] = new RenderTexture(rasterizationResolutions[pass].x, rasterizationResolutions[pass].y, 24);
            rasterized[pass].format = RenderTextureFormat.ARGBFloat;

            GameObject obj = new GameObject($"RenderCamera{pass}");
            obj.transform.parent = transform;
            obj.transform.position = Vector3.zero;
            renderCameras[pass] = obj.AddComponent<Camera>();
            renderCameras[pass].clearFlags = CameraClearFlags.SolidColor;
            renderCameras[pass].backgroundColor = Color.clear;
            renderCameras[pass].cullingMask = 1 << LayerMask.NameToLayer("Rasterized");
            renderCameras[pass].targetTexture = rasterized[pass];

            // copy some flags for comfort
            renderCameras[pass].useOcclusionCulling = originalCamera.useOcclusionCulling;
            renderCameras[pass].allowHDR = originalCamera.allowHDR;
            renderCameras[pass].allowMSAA = originalCamera.allowMSAA;
            renderCameras[pass].allowDynamicResolution = originalCamera.allowDynamicResolution;
        }
        
        rasterizationMaterial = new Material(rasterizationShader);
        postRasterizationMaterial = new Material(postRasterizationShader);
        postRasterizationMaterial.SetVector("RESOLUTION", new Vector2(rasterizationResolutions[passes - 1].x, rasterizationResolutions[passes - 1].y));
        postRasterizationMaterial.SetTexture("_Input", input);
        postRasterizationMaterial.SetTexture("_Coordinates", rasterized[passes - 1]);

        renderParams = new RenderParams(rasterizationMaterial);
        renderParams.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds
        renderParams.matProps = new MaterialPropertyBlock();
        renderParams.layer = 3;
        /*
         * only needed for external meshes
        renderParams.matProps.SetInt("_StartIndex", (int)mesh.GetIndexStart(0));
        renderParams.matProps.SetInt("_BaseVertexIndex", (int)mesh.GetBaseVertex(0));
        */
        // might be needed later for multiple viewpoints
        renderParams.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(meshTranslation));

        for (int pass = 0; pass < passes; pass++)
        {

            computeShader.SetInt("RENDER_PASS", pass);
            computeShader.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolutions[pass].x, projectionResolutions[pass].y));
            computeShader.SetBuffer(loadTexelsToQuadBufferKernel, "_Triangles", meshTriangles[pass]);
            computeShader.SetBuffer(loadTexelsToQuadBufferKernel, "_Positions", meshPositions[pass]);
            computeShader.SetBuffer(loadTexelsToQuadBufferKernel, "_UVs", meshUVs[pass]);

            if (pass != 0)
            {
                if (pass == 1)
                {
                    computeShader.EnableKeyword("USE_PREVIOUS_PASS");
                }
                computeShader.SetVector("PREVIOUS_RASTERIZATION_RESOLUTION", new Vector2(rasterizationResolutions[pass - 1].x, rasterizationResolutions[pass - 1].y));
                computeShader.SetTexture(loadTexelsToQuadBufferKernel, "_PreviousPass", rasterized[pass - 1]);
            }
            computeShader.Dispatch(loadTexelsToQuadBufferKernel, projectionResolutions[pass].x / (int)threadGroupSizeX, projectionResolutions[pass].y / (int)threadGroupSizeY, 1);
        }
    }

    void Update()
    {
        renderParams.matProps.SetFloat("TIMESTEP", Time.frameCount + Time.deltaTime);
        renderParams.matProps.SetFloat("FCLIP", fClip - fClipCutoff);
        renderParams.matProps.SetFloat("MAX_CIRCUMFERENCE", maxCircumference);


        for (int pass = 0; pass < passes; pass++)
        {

            if (pass != 0)
            {
                computeShader.SetInt("RENDER_PASS", pass);
                computeShader.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolutions[pass].x, projectionResolutions[pass].y));
                computeShader.SetBuffer(loadTexelsToQuadBufferKernel, "_Triangles", meshTriangles[pass]);
                computeShader.SetBuffer(loadTexelsToQuadBufferKernel, "_Positions", meshPositions[pass]);
                computeShader.SetBuffer(loadTexelsToQuadBufferKernel, "_UVs", meshUVs[pass]);

                if (pass == 1)
                {
                    computeShader.EnableKeyword("USE_PREVIOUS_PASS");
                }
                computeShader.SetVector("PREVIOUS_RASTERIZATION_RESOLUTION", new Vector2(rasterizationResolutions[pass - 1].x, rasterizationResolutions[pass - 1].y));
                computeShader.SetTexture(loadTexelsToQuadBufferKernel, "_PreviousPass", rasterized[pass - 1]);

                computeShader.Dispatch(loadTexelsToQuadBufferKernel, projectionResolutions[pass].x / (int)threadGroupSizeX, projectionResolutions[pass].y / (int)threadGroupSizeY, 1);
            }

            renderParams.matProps.SetInt("DEBUG_MODE", (int)debugMode);
            renderParams.matProps.SetInt("RENDER_PASS", pass);
            renderParams.matProps.SetTexture("_Input", input);
            renderParams.matProps.SetBuffer("_Triangles", meshTriangles[pass]);
            renderParams.matProps.SetBuffer("_Positions", meshPositions[pass]);
            renderParams.matProps.SetBuffer("_UVs", meshUVs[pass]);
            renderParams.camera = renderCameras[pass];

            // (int)mesh.GetIndexCount(0) for external meshes
            // maybe switch to using quad topology
            Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, indicies[pass]);
        }

        if (passes != 1)
        {
            computeShader.DisableKeyword("USE_PREVIOUS_PASS");
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        switch (debugChannel)
        {
            case DebugChannel.motionVectors:
                Graphics.Blit(motionVectors, destination);
                break;
            case DebugChannel.rasterized:
                Graphics.Blit(rasterized[Mathf.Max(0, Mathf.Min(passes - 1, debugPass))], destination);
                break;
            default:
                Graphics.Blit(source, destination, postRasterizationMaterial);
                break;
        }
    }

    void OnDestroy()
    {
        for (int pass = 0; pass < passes; pass++)
        {
            meshTriangles[pass]?.Dispose();
            meshTriangles[pass] = null;
            meshPositions[pass]?.Dispose();
            meshPositions[pass] = null;
            meshUVs[pass]?.Dispose();
            meshUVs[pass] = null;
        }
    }
}
