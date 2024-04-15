using PreRendering;
using UnityEngine;

public class CubemapLoader : MonoBehaviour
{
    public ComputeShader vertexGenerator;
    public Shader initialApproximationShader, progressiveApproximationShader, rasterizationShader;
    public Texture2D[] cubemapTextures;
    public RenderTexture[] approximationTextures;
    public Camera rasterizationCamera;
    public Camera[] approximationCameras;

    [Range(1, 4)]
    public int approximationLayers = 2;
    [Range(1, 20)]
    public int approximationDownsamplingRatio = 4;
    public float maxDepthDifference, projectionDifference;
    public Vector4[] cubePositions;

    private Material rasterizationMaterial;
    private Material[] approximationMaterials;
    private RenderParams renderParams;
    private int indicies;

    private const string CullingMaskLayerName = "Rasterization Pass";
    private const int TriangulationVertexRatio = 6;
    private const int ApproximationLayerStartIndex = 6;

    private int GetApproximationLayerMask(int index) => 1 << (index + ApproximationLayerStartIndex);

    private void Start()
    {
        // lock cursor
        Cursor.lockState = CursorLockMode.Locked;

        var rasterizationResolution = new Vector2Int(Screen.width / approximationDownsamplingRatio, Screen.height / approximationDownsamplingRatio);

        // initialize materials
        approximationMaterials = new Material[approximationLayers];
        approximationTextures = new RenderTexture[approximationLayers];
        approximationCameras = new Camera[approximationLayers];
        for (int i = 0; i < approximationLayers; i++)
        {
            var shader = i == 0 ? initialApproximationShader : progressiveApproximationShader;
            approximationMaterials[i] = new Material(shader);
            approximationMaterials[i].SetVectorArray("CUBE_POSITIONS", cubePositions);
            approximationMaterials[i].SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);
            if (i != 0)
            {
                approximationMaterials[i].SetTexture("_PreviousApproximation", approximationTextures[i - 1]);
            }

            approximationTextures[i] = new RenderTexture(rasterizationResolution.x, rasterizationResolution.y, 24, RenderTextureFormat.ARGBFloat, 0);
            approximationTextures[i].filterMode = FilterMode.Point;
            
            var approximationCameraObject = new GameObject($"ApproximationCamera_Layer{i}");
            approximationCameraObject.transform.parent = rasterizationCamera.transform.parent;
            approximationCameraObject.transform.localPosition = Vector3.zero;
            approximationCameras[i] = approximationCameraObject.AddComponent<Camera>();
            approximationCameras[i].clearFlags = CameraClearFlags.SolidColor;
            approximationCameras[i].backgroundColor = Color.clear;
            approximationCameras[i].cullingMask = GetApproximationLayerMask(i);
            approximationCameras[i].fieldOfView = 90;
            approximationCameras[i].nearClipPlane = 0.2f;
            approximationCameras[i].farClipPlane = 500;
            approximationCameras[i].depth = -approximationLayers + i;
            approximationCameras[i].targetTexture = approximationTextures[i];
            approximationCameras[i].allowHDR = false;
            approximationCameras[i].allowMSAA = false;
        }

        // apply material to all rendered meshes
        var meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        foreach (var renderer in meshRenderers)
        {
            if (renderer.tag != "RenderTarget")
            {
                continue;
            }

            for (int i = 1; i < approximationLayers; i++)
            {
                var layerRenderer = Instantiate(renderer);
                layerRenderer.name = $"{renderer.name}_Layer{i}";
                layerRenderer.gameObject.layer = ApproximationLayerStartIndex + i;

                // remove obsolete colliders
                Destroy(layerRenderer.GetComponent<Collider>());

                // reset transform
                layerRenderer.transform.position = renderer.transform.position;
                layerRenderer.transform.rotation = renderer.transform.rotation;
                layerRenderer.transform.parent = renderer.transform;

                // apply material
                layerRenderer.material = approximationMaterials[i];
            }

            renderer.name += $"_Layer0";
            renderer.material = approximationMaterials[0];
            renderer.gameObject.layer = ApproximationLayerStartIndex;
        }
        
        // load cubemap textures to gpu
        var sampleTexture = cubemapTextures[0];
        var cubemapTextureArray = new Texture2DArray(sampleTexture.width, sampleTexture.height, cubemapTextures.Length, sampleTexture.format, false);
        for (int textureIndex = 0; textureIndex < cubemapTextures.Length; textureIndex++)
        {
            Graphics.CopyTexture(cubemapTextures[textureIndex], 0, cubemapTextureArray, textureIndex);
        }

        Matrix4x4 VP = GL.GetGPUProjectionMatrix(approximationCameras[0].projectionMatrix, false) * approximationCameras[0].worldToCameraMatrix;
        indicies = (rasterizationResolution.x - 1) * (rasterizationResolution.y - 1) * TriangulationVertexRatio;

        // set render params
        var renderMatProps = new MaterialPropertyBlock();
        renderMatProps.SetVector("SCREEN_RESOLUTION", new Vector2(rasterizationResolution.x, rasterizationResolution.y));
        renderMatProps.SetVector("TARGET_TEXTURE_RESOLUTION", rasterizationResolution.ToVector2());
        renderMatProps.SetMatrix("VP_I", VP.inverse);
        renderMatProps.SetMatrixArray("ORIENTATION_MATRICIES", CubeMapConversion.orientationMatricies);
        renderMatProps.SetTexture("_CubemapTextures", cubemapTextureArray);
        for (int i = 0; i < approximationLayers; i++)
        {
            renderMatProps.SetTexture($"_ApproximationTexture{i}", approximationTextures[i]);
        }

        int cullingMaskLayer = LayerMask.NameToLayer(CullingMaskLayerName);

        rasterizationMaterial = new Material(rasterizationShader);
        renderParams = new RenderParams(rasterizationMaterial)
        {
            worldBounds = new Bounds(Vector3.zero, 100 * Vector3.one), // use tighter bounds
            camera = rasterizationCamera,
            matProps = renderMatProps,
            layer = cullingMaskLayer
        };
    }

    private void Update()
    {
        renderParams.matProps.SetFloat("MAX_DEPTH_DIFFERENCE", maxDepthDifference);
        renderParams.matProps.SetFloat("PROJ_DIFF", projectionDifference);
        renderParams.matProps.SetVectorArray("CUBE_POSITIONS", cubePositions);

        Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, indicies);
    }
}
