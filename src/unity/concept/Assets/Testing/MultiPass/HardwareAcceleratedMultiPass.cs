using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HardwareAcceleratedMultiPass : MonoBehaviour
{
    const int MAX_PASSES = 4;

    public Texture2D input;
    public ComputeShader computeShader;
    public Shader rasterizationShader, postRasterizationShader;
    public float nClip, fClip;
    public float fClipCutoff = 1;
    [Range(1, MAX_PASSES)]
    public int passes = 1;
    public Vector2Int[] projectionResolutions, rasterizationResolutions;
    public Vector3 meshTranslation;
    public Camera[] renderCameras;

    public Material rasterizationMaterial, postRasterizationMaterial;
    private RenderParams renderParams;
    private GraphicsBuffer[] meshTriangles, meshPositions, meshUVs;
    public RenderTexture[] rasterized;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int loadTexelsToQuadBuffer;
    private int[] verticies, indicies;

    void Start()
    {
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

        loadTexelsToQuadBuffer = computeShader.FindKernel("LoadTexelsToQuadBuffer");
        computeShader.GetKernelThreadGroupSizes(loadTexelsToQuadBuffer, out threadGroupSizeX, out threadGroupSizeY, out _);
        computeShader.SetTexture(loadTexelsToQuadBuffer, "_Input", input);

        for (int pass = 0; pass < passes; pass++)
        {
            verticies[pass] = projectionResolutions[pass].x * projectionResolutions[pass].y;
            indicies[pass] = verticies[pass] * 6;

            meshTriangles[pass] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indicies[pass], sizeof(int));
            meshPositions[pass] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies[pass], 3 * sizeof(float));
            meshUVs[pass] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies[pass], 2 * sizeof(float));

            rasterized[pass] = new RenderTexture(rasterizationResolutions[pass].x, rasterizationResolutions[pass].y, 0);
            rasterized[pass].format = RenderTextureFormat.ARGBFloat;

            GameObject obj = new GameObject($"RenderCamera{pass}");
            obj.transform.parent = transform;
            obj.transform.position = Vector3.zero;
            renderCameras[pass] = obj.AddComponent<Camera>();
            renderCameras[pass].clearFlags = CameraClearFlags.SolidColor;
            renderCameras[pass].backgroundColor = Color.clear;
            renderCameras[pass].cullingMask = 1 << LayerMask.NameToLayer("Rasterized");
            renderCameras[pass].targetTexture = rasterized[pass];

            // copy some other flags for comfort
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
    }

    void Update()
    {
        renderParams.matProps.SetFloat("TIMESTEP", Time.frameCount + Time.deltaTime);
        renderParams.matProps.SetFloat("FCLIP", fClip - fClipCutoff);


        for (int pass = 0; pass < passes; pass++)
        {
            computeShader.SetInt("RENDER_PASS", pass);
            computeShader.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolutions[pass].x, projectionResolutions[pass].y));
            computeShader.SetBuffer(loadTexelsToQuadBuffer, "_Triangles", meshTriangles[pass]);
            computeShader.SetBuffer(loadTexelsToQuadBuffer, "_Positions", meshPositions[pass]);
            computeShader.SetBuffer(loadTexelsToQuadBuffer, "_UVs", meshUVs[pass]);
            if (pass != 0)
            {
                if (pass == 1)
                {
                    computeShader.EnableKeyword("USE_PREVIOUS_PASS");
                }
                computeShader.SetVector("PREVIOUS_RASTERIZATION_RESOLUTION", new Vector2(rasterizationResolutions[pass - 1].x, rasterizationResolutions[pass - 1].y));
                computeShader.SetTexture(loadTexelsToQuadBuffer, "_PreviousPass", rasterized[pass - 1]);
            }

            computeShader.Dispatch(loadTexelsToQuadBuffer, projectionResolutions[pass].x / (int)threadGroupSizeX, projectionResolutions[pass].y / (int)threadGroupSizeY, 1);

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
        Graphics.Blit(source, destination, postRasterizationMaterial);
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
