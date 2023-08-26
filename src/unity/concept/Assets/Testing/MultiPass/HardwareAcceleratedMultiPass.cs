using UnityEngine;

public class HardwareAcceleratedMultiPass : MonoBehaviour
{
    public ComputeShader computeShader;
    public Shader shader;

    private Material material;
    private RenderParams renderParams;
    private GraphicsBuffer meshTriangles, meshPositions, meshUVs;

    const int tris = 2;
    const int verticies = 4;
    const int indexCount = 3 * tris;

    void Start()
    {
        meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indexCount, sizeof(int));
        meshPositions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 3 * sizeof(float));
        meshUVs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 2 * sizeof(float));

        int groupQuadsKernel = computeShader.FindKernel("GroupQuads");
        computeShader.SetBuffer(groupQuadsKernel, "_Triangles", meshTriangles);
        computeShader.SetBuffer(groupQuadsKernel, "_Positions", meshPositions);
        computeShader.SetBuffer(groupQuadsKernel, "_UVs", meshUVs);
        computeShader.Dispatch(groupQuadsKernel, 1, 1, 1);

        material = new Material(shader);
        renderParams = new RenderParams(material);
        renderParams.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds
        renderParams.matProps = new MaterialPropertyBlock();
        renderParams.matProps.SetBuffer("_Triangles", meshTriangles);
        renderParams.matProps.SetBuffer("_Positions", meshPositions);
        renderParams.matProps.SetBuffer("_UVs", meshUVs);
        /*
         * only needed for external meshes
        renderParams.matProps.SetInt("_StartIndex", (int)mesh.GetIndexStart(0));
        renderParams.matProps.SetInt("_BaseVertexIndex", (int)mesh.GetBaseVertex(0));
        */
        // might be needed later for multiple viewpoints
        renderParams.matProps.SetMatrix("_ObjectToWorld", Matrix4x4.Translate(new Vector3(0, 0, 0)));
    }

    void Update()
    {
        // (int)mesh.GetIndexCount(0) for external meshes
        Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, indexCount);
    }

    void OnDestroy()
    {
        meshTriangles?.Dispose();
        meshTriangles = null;
        meshPositions?.Dispose();
        meshPositions = null;
        meshUVs?.Dispose();
        meshUVs = null;
    }
}
