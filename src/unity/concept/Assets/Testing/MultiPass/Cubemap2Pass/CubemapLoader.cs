using PreRendering;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CubemapLoader : MonoBehaviour
{
    public ComputeShader vertexGenerator;
    public Material cubemapApproximationMaterial, cubemapRasterizationMaterial;
    public Texture2D[] cubemapTextures;
    public RenderTexture approximationTargetTexture;
    public Camera approximationCamera, rasterizationCamera;
    [Range(1, 20)]
    public int approximationDownsamplingRatio = 4;

    private RenderParams renderParams;
    private int indicies;

    private const string CullingMaskLayerName = "Rasterized";
    private const int TriangulationVertexRatio = 6;

    private void Start()
    {
        // apply material to all rendered meshes
        var meshRenderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        foreach (var renderer in meshRenderers)
        {
            if (renderer.tag == "GameController") continue;
            renderer.material = cubemapApproximationMaterial;
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

        // int vertexCount = sampleTexture.width * sampleTexture.height;
        // int[] indices = new int[(sampleTexture.width - 1) * (sampleTexture.height - 1) * 6]; // 6 indices per quad

        /*
        // assign mesh to MeshFilter component
        int vertexCount = sampleTexture.width * sampleTexture.height;
        var meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = GenerateDummyMesh(rasterizationResolution);
        */
        indicies = (rasterizationResolution.x - 1) * (rasterizationResolution.y - 1) * TriangulationVertexRatio;

        // set render params
        var renderMatProps = new MaterialPropertyBlock();
        renderMatProps.SetVector("SCREEN_RESOLUTION", new Vector2(Screen.width, Screen.height));
        renderMatProps.SetVector("TARGET_TEXTURE_RESOLUTION", rasterizationResolution.ToVector2());
        renderMatProps.SetMatrix("VP_I", VP.inverse);
        renderMatProps.SetTexture("_ApproximationTargetTexture", approximationTargetTexture);
        renderMatProps.SetTexture("_CubemapTextures", cubemapTextureArray);

        int cullingMaskLayer = LayerMask.NameToLayer(CullingMaskLayerName);

        renderParams = new RenderParams(cubemapRasterizationMaterial)
        {
            worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one), // use tighter bounds
            camera = rasterizationCamera,
            matProps = renderMatProps,
            layer = cullingMaskLayer
        };
    }

    private void Update()
    {
        Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, indicies);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // rasterizationCamera.targetTexture = destination;
        // Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, 3);
        // Graphics.Blit(rasterizationTexture, destination);
    }

    private void OnDestroy()
    {
        
    }
    /*
    private Mesh GenerateDummyMesh(Vector2Int resolution)
    {
        int vertexCount = Mathf.RoundToInt((resolution.x - 1) * (resolution.y - 1));
        // int[] indices = Enumerable.Range(0, vertexCount * TriangulationVertexRatio).ToArray();
        int[] indices = new int[vertexCount * TriangulationVertexRatio];
        for (int i = 0; i < indices.Length; i++)
        {
            int faceIndex = i % TriangulationVertexRatio;
            int vertexIndex = (i - faceIndex) / TriangulationVertexRatio;

            switch (faceIndex)
            {
                // case 0 stays unmodified (0, 0)
                case 1:                 // (1, 1)
                    vertexIndex += resolution.x + 1;
                    break;
                case 2:                 // (1, 0)
                    vertexIndex += 1;
                    break;
                case 3:                 // (1, 1)
                    vertexIndex += resolution.x + 1;
                    vertexIndex = 0;
                    break;
                // case 4 stays unmodified (0, 0)
                case 4:                 // (1, 1)
                    vertexIndex = 0;
                    break;
                case 5:                 // (0, 1)
                    vertexIndex += resolution.x;
                    vertexIndex = 0;
                    break;
            }

            if (vertexIndex >= vertexCount && false)
            {
                vertexIndex = 0;
            }
            indices[i] = vertexIndex;
        }

        var mesh = new Mesh();
        mesh.SetVertices(new Vector3[resolution.x * resolution.y]);
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);

        return mesh;
    }
    */
}
