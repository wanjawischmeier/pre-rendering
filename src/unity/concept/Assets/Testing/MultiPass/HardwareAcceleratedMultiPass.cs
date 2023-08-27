using NUnit.Framework.Internal;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class HardwareAcceleratedMultiPass : MonoBehaviour
{
    public Texture2D input;
    public ComputeShader computeShader;
    public Shader rasterizationShader, postRasterizationShader;
    public float nClip, fClip;
    public Vector2Int projectionResolution, rasterizationResolution;
    public Camera renderCamera;

    private Material rasterizationMaterial, postRasterizationMaterial;
    private RenderParams renderParams;
    private GraphicsBuffer meshTriangles, meshPositions, meshUVs;
    public RenderTexture rasterized;
    private int verticies, indicies;

    void Start()
    {
        verticies = projectionResolution.x * projectionResolution.y;
        indicies = verticies * 6;

        meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indicies, sizeof(int));
        meshPositions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 3 * sizeof(float));
        meshUVs = new GraphicsBuffer(GraphicsBuffer.Target.Structured, verticies, 2 * sizeof(float));
        
        int loadTexelsToQuadBuffer = computeShader.FindKernel("LoadTexelsToQuadBuffer");
        computeShader.GetKernelThreadGroupSizes(loadTexelsToQuadBuffer, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);

        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", nClip);
        computeShader.SetFloat("FCLIP", fClip);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetTexture(loadTexelsToQuadBuffer, "_Input", input);
        computeShader.SetBuffer(loadTexelsToQuadBuffer, "_Triangles", meshTriangles);
        computeShader.SetBuffer(loadTexelsToQuadBuffer, "_Positions", meshPositions);
        computeShader.SetBuffer(loadTexelsToQuadBuffer, "_UVs", meshUVs);

        computeShader.SetVector("PROJECTION_RESOLUTION", new Vector2(projectionResolution.x, projectionResolution.y));
        computeShader.Dispatch(loadTexelsToQuadBuffer, projectionResolution.x / (int)threadGroupSizeX, projectionResolution.y / (int)threadGroupSizeY, 1);

        rasterized = new RenderTexture(rasterizationResolution.x, rasterizationResolution.y, 0);
        renderCamera.targetTexture = rasterized;

        rasterizationMaterial = new Material(rasterizationShader);
        postRasterizationMaterial = new Material(postRasterizationShader);
        postRasterizationMaterial.SetVector("RESOLUTION", new Vector2(rasterizationResolution.x, rasterizationResolution.y));
        postRasterizationMaterial.SetTexture("_Input", input);
        postRasterizationMaterial.SetTexture("_Coordinates", rasterized);

        renderParams = new RenderParams(rasterizationMaterial);
        renderParams.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds
        renderParams.matProps = new MaterialPropertyBlock();
        renderParams.matProps.SetTexture("_Input", input);
        renderParams.matProps.SetBuffer("_Triangles", meshTriangles);
        renderParams.matProps.SetBuffer("_Positions", meshPositions);
        renderParams.matProps.SetBuffer("_UVs", meshUVs);
        renderParams.camera = renderCamera;
        renderParams.layer = 3;
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
        renderParams.matProps.SetFloat("TIMESTEP", Time.frameCount + Time.deltaTime);
        
        // (int)mesh.GetIndexCount(0) for external meshes
        // maybe switch to using quad topology
        Graphics.RenderPrimitives(renderParams, MeshTopology.Triangles, indicies);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, postRasterizationMaterial);
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
