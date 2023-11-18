using PreRendering;
using UnityEngine;

public class UnityRP : MonoBehaviour
{
    public ComputeShader loadTexels;
    public Shader shader, computeWorldSpacePositionShader;
    public Material finalMaterial;
    public Camera renderCamera;
    public Texture input0, input1, input2;
    public Vector2Int projectionResolution;
    public Vector3 meshTranslation;
    public Transform[] pointTransforms;
    public DynamicRenderBuffer.DebugMode debugMode;

    private int loadTexelsKernelId, verticies, indicies, cullingMaskLayer;
    private int loadTexelsToBufferGroupSizeX, loadTexelsToBufferGroupSizeY;
    private Camera mainCamera;
    private Material material, computeWorldSpacePositionMaterial;
    private RenderTexture target;
    private GraphicsBuffer triangles, positions, uvs;
    private RenderParams renderParams;
    private Matrix4x4 projMat, viewMat;
    private Vector4[] points;

    private const string CullingMaskLayerName = "Rasterized";

    void Start()
    {
        verticies = projectionResolution.x * projectionResolution.y;
        indicies = verticies * 6;

        points = new Vector4[pointTransforms.Length];

        cullingMaskLayer = LayerMask.NameToLayer(CullingMaskLayerName);
        material = new Material(shader);
        computeWorldSpacePositionMaterial = new Material(computeWorldSpacePositionShader);
        computeWorldSpacePositionMaterial.SetFloat("PI", Mathf.PI);
        computeWorldSpacePositionMaterial.SetTexture("_Input0", input0);
        computeWorldSpacePositionMaterial.SetTexture("_Input1", input1);
        computeWorldSpacePositionMaterial.SetTexture("_Input2", input2);

        target = new RenderTexture(projectionResolution.x, projectionResolution.y, 24);
        target.format = RenderTextureFormat.Depth;
        mainCamera = Camera.main;
        renderCamera.targetTexture = target;
        renderCamera.depthTextureMode = DepthTextureMode.Depth;

        projMat = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, false);

        triangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indicies, sizeof(int));
        positions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 3 * sizeof(float));
        uvs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 2 * sizeof(float));

        loadTexelsKernelId = loadTexels.FindKernel("LoadTexelsToBuffer");
        loadTexels.GetKernelThreadGroupSizes(loadTexelsKernelId, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        loadTexelsToBufferGroupSizeX = projectionResolution.x / (int)threadGroupSizeX;
        loadTexelsToBufferGroupSizeY = projectionResolution.y / (int)threadGroupSizeY;
        loadTexels.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolution.x, projectionResolution.y));
        loadTexels.SetTexture(loadTexelsKernelId, "_InitialPass", target);
        loadTexels.SetBuffer(loadTexelsKernelId, "_Triangles", triangles);
        loadTexels.SetBuffer(loadTexelsKernelId, "_Positions", positions);
        loadTexels.SetBuffer(loadTexelsKernelId, "_UVs", uvs);
        loadTexels.SetTexture(loadTexelsKernelId, "_CameraDepthTexture", target);


        var renderMatProps = new MaterialPropertyBlock();
        renderMatProps.SetBuffer("_Triangles", triangles);
        renderMatProps.SetBuffer("_Positions", positions);
        renderMatProps.SetBuffer("_UVs", uvs);

        material = new Material(shader);
        renderParams = new RenderParams(material)
        {
            worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one), // use tighter bounds
            camera = renderCamera,
            matProps = renderMatProps,
            layer = cullingMaskLayer
        };
    }

    void Update()
    {
        viewMat = mainCamera.worldToCameraMatrix;
        Matrix4x4 objToWorldMat = Matrix4x4.Translate(meshTranslation);
        Matrix4x4 viewProjInvMat = (projMat * viewMat).inverse;
        loadTexels.SetMatrix("_ObjectToWorld", objToWorldMat);
        loadTexels.SetMatrix("_ViewProjInv", viewProjInvMat);
        computeWorldSpacePositionMaterial.SetMatrix("_ViewProjInv", viewProjInvMat);

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = pointTransforms[i].position;
        }
        computeWorldSpacePositionMaterial.SetVectorArray("POINTS", points);
        computeWorldSpacePositionMaterial.SetVector("PCAM", mainCamera.transform.position);

        loadTexels.SetFloat("TIMESTEP", Time.time);
        loadTexels.Dispatch(loadTexelsKernelId, loadTexelsToBufferGroupSizeX, loadTexelsToBufferGroupSizeY, 1);

        renderParams.matProps.SetInt("DEBUG_MODE", (int)debugMode);
        renderParams.matProps.SetFloat("TIMESTEP", Time.time);
        renderParams.matProps.SetMatrix("_ObjectToWorld", objToWorldMat);
        Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, indicies);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, computeWorldSpacePositionMaterial);
    }

    private void OnDestroy()
    {
        target.Release();
        triangles.Dispose();
        positions.Dispose();
        uvs.Dispose();
    }
}
