using PreRendering;
using UnityEngine;

public class CubemapLoader : MonoBehaviour
{
    public ComputeShader vertexGenerator;
    public Material cubemapApproximationMaterial, cubemapRasterizationMaterial;
    public Texture2D[] cubemapTextures;
    public RenderTexture approximationTargetTexture;
    public Camera approximationCamera, rasterizationCamera;
    [Range(1, 20)]
    public int approximationDownsamplingRatio = 4;
    public float maxDepthDifference;
    public Vector3 mapOffset;
    public Vector4[] cubePositions;

    private RenderParams renderParams;
    private int indicies;

    private const string CullingMaskLayerName = "Rasterized";
    private const int TriangulationVertexRatio = 6;

    private void Start()
    {
        // lock cursor
        Cursor.lockState = CursorLockMode.Locked;

        // apply material to all rendered meshes
        var meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        foreach (var renderer in meshRenderers)
        {
            // renderer.material = cubemapApproximationMaterial;
        }
        
        // load cubemap textures to gpu
        var sampleTexture = cubemapTextures[0];
        var cubemapTextureArray = new Texture2DArray(sampleTexture.width, sampleTexture.height, cubemapTextures.Length, sampleTexture.format, false);
        for (int textureIndex = 0; textureIndex < cubemapTextures.Length; textureIndex++)
        {
            Graphics.CopyTexture(cubemapTextures[textureIndex], 0, cubemapTextureArray, textureIndex);
        }

        var rasterizationResolution = new Vector2Int(Screen.width / approximationDownsamplingRatio, Screen.height / approximationDownsamplingRatio);
        approximationTargetTexture = new RenderTexture(rasterizationResolution.x, rasterizationResolution.y, 24, RenderTextureFormat.ARGBFloat, 0);
        approximationTargetTexture.filterMode = FilterMode.Point;
        approximationCamera.targetTexture = approximationTargetTexture;

        Matrix4x4 VP = GL.GetGPUProjectionMatrix(approximationCamera.projectionMatrix, false) * approximationCamera.worldToCameraMatrix;

        cubemapApproximationMaterial.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);

        indicies = (rasterizationResolution.x - 1) * (rasterizationResolution.y - 1) * TriangulationVertexRatio;

        // set render params
        var renderMatProps = new MaterialPropertyBlock();
        renderMatProps.SetVector("SCREEN_RESOLUTION", new Vector2(Screen.width, Screen.height));
        renderMatProps.SetVector("TARGET_TEXTURE_RESOLUTION", rasterizationResolution.ToVector2());
        renderMatProps.SetMatrix("VP_I", VP.inverse);
        renderMatProps.SetMatrixArray("ORIENTATION_MATRICIES", CubeMapConversion.orientationMatricies);
        renderMatProps.SetTexture("_ApproximationTargetTexture", approximationTargetTexture);
        renderMatProps.SetTexture("_CubemapTextures", cubemapTextureArray);

        int cullingMaskLayer = LayerMask.NameToLayer(CullingMaskLayerName);

        renderParams = new RenderParams(cubemapRasterizationMaterial)
        {
            worldBounds = new Bounds(Vector3.zero, 100 * Vector3.one), // use tighter bounds
            camera = rasterizationCamera,
            matProps = renderMatProps,
            layer = cullingMaskLayer
        };
    }

    private void Update()
    {
        cubemapApproximationMaterial.SetVector("OFF", mapOffset);
        renderParams.matProps.SetFloat("MAX_DEPTH_DIFFERENCE", maxDepthDifference);
        renderParams.matProps.SetVectorArray("CUBE_POSITIONS", cubePositions);

        Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, indicies);
    }
}
