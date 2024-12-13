using UnityEngine;

public class DemoMeshLoader : MonoBehaviour
{
    public RenderTexture initialPass;
    public Material meshMaterial;
    public Camera meshCamera;

    private int indicies;
    private RenderParams renderParams;

    private const int TriangulationVertexRatio = 6;

    private void Start()
    {
        indicies = (initialPass.width - 1) * (initialPass.height - 1) * TriangulationVertexRatio;

        // set render params
        var renderMatProps = new MaterialPropertyBlock();
        renderMatProps.SetTexture("_InitialPass", initialPass);
        renderMatProps.SetVector("TARGET_TEXTURE_RESOLUTION", new Vector2(initialPass.width, initialPass.height));

        renderParams = new RenderParams(meshMaterial)
        {
            worldBounds = new Bounds(Vector3.zero, 100 * Vector3.one), // use tighter bounds
            camera = meshCamera,
            matProps = renderMatProps
        };
    }

    private void Update()
    {
        Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, indicies);
    }
}
