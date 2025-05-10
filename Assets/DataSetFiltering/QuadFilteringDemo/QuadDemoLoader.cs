using UnityEngine;
using UnityEngine.Rendering;

public class QuadDemoLoader : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material softwareRasterDebug, hardwareRasterDebug, hardwareRasterMaterial;
    public Texture2D inputTexture;
    public RenderTexture transformedVertexTexture, vertexLookupTexture, rasterOutputTexture;
    public float nearClip, farClip;
    public Vector2Int rescaledInputResolution, dispatchResolution, transformedVertexResolution;
    public Vector2 uvOffset;
    public uint[] args;
    public int maxHardwareRasterizedTriangles = 32000;
    private ComputeBuffer vertexBuffer, argsBuffer;

    private int transformVerticesKernelHandle, renderQuadsKernelHandle;

    private void Start()
    {
        if (inputTexture.width != rescaledInputResolution.x || inputTexture.height != rescaledInputResolution.y)
        {
            /*
            // Rescale the input texture to the desired resolution
            Texture2D rescaledInputTexture = new Texture2D(rescaledInputResolution.x, rescaledInputResolution.y, TextureFormat.RGBA32, false);
            Graphics.ConvertTexture(inputTexture, rescaledInputTexture);
            inputTexture = rescaledInputTexture;
            */
        }

        transformedVertexTexture = new RenderTexture(transformedVertexResolution.x, transformedVertexResolution.y, 0, RenderTextureFormat.RInt);
        transformedVertexTexture.enableRandomWrite = true;
        transformedVertexTexture.Create();

        vertexLookupTexture = new RenderTexture(transformedVertexTexture);
        vertexLookupTexture.format = RenderTextureFormat.RGInt;
        vertexLookupTexture.Create();

        rasterOutputTexture = new RenderTexture(transformedVertexResolution.x, transformedVertexResolution.y, 0, RenderTextureFormat.ARGB32);
        rasterOutputTexture.Create();
        hardwareRasterDebug.mainTexture = rasterOutputTexture;

        vertexBuffer = new ComputeBuffer(maxHardwareRasterizedTriangles, 9 * sizeof(float), ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, 4 * sizeof(int), ComputeBufferType.IndirectArguments);

        transformVerticesKernelHandle = computeShader.FindKernel("TranformVertices");
        renderQuadsKernelHandle = computeShader.FindKernel("RenderQuads");
        computeShader.SetVector("_InputResolution", new Vector2(inputTexture.width, inputTexture.height));
        computeShader.SetVector("_DispatchResolution", new Vector2(dispatchResolution.x, dispatchResolution.y));
        computeShader.SetVector("_OutputResolution", new Vector2(transformedVertexResolution.x, transformedVertexResolution.y));
        computeShader.SetMatrixArray("_OrientationMatricies", CubeMapConversion.orientationMatricies);
        computeShader.SetTexture(transformVerticesKernelHandle, "_InputColorBuffer", inputTexture);
        computeShader.SetTexture(transformVerticesKernelHandle, "RW_OutputDepthBuffer", transformedVertexTexture);

        softwareRasterDebug.SetVector("_InputResolution", new Vector2(transformedVertexResolution.x, transformedVertexResolution.y));
        softwareRasterDebug.SetTexture("_InputDepthBuffer", transformedVertexTexture);

        hardwareRasterMaterial.SetBuffer("_Vertices", vertexBuffer);
    }

    private void Update()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = transformedVertexTexture;
        GL.Clear(true, true, new Color(int.MaxValue, 0, 0));
        RenderTexture.active = rt;

        vertexBuffer.SetCounterValue(0); // Reset append buffer

        Matrix4x4 trs = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, true);
        Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
        computeShader.SetMatrix("_CameraViewProj", viewProjMatrix);

        computeShader.SetFloat("_MapNearClip", nearClip);
        computeShader.SetFloat("_MapFarClip", farClip);
        computeShader.SetVector("_UVOffset", uvOffset);
        computeShader.SetVector("_Offset", transform.position);
        computeShader.SetBuffer(transformVerticesKernelHandle, "_HardwareVertices", vertexBuffer);
        computeShader.Dispatch(transformVerticesKernelHandle, dispatchResolution.x / 8, dispatchResolution.y / 8, 1);
        
        // Copy count from append buffer to args[0] (vertex count)
        ComputeBuffer.CopyCount(vertexBuffer, argsBuffer, 0);

        args = new uint[] { 0, 1, 0, 0 };
        argsBuffer.GetData(args);
        args[0] *= 3; // vertex count = triangle count * 3
        args[1] = 1;
        argsBuffer.SetData(args);

        // Set hardware raster material parameters
        hardwareRasterMaterial.SetFloat("_CameraNearClip", Camera.main.nearClipPlane);
        hardwareRasterMaterial.SetFloat("_CameraFarClip", Camera.main.farClipPlane);

        var cmd = new CommandBuffer();
        /*
        // Set parameters and dispatch compute shader (can run in parallel)
        cmd.SetComputeFloatParam(computeShader, "_MapNearClip", nearClip);
        cmd.SetComputeFloatParam(computeShader, "_MapFarClip", farClip);
        cmd.SetComputeVectorParam(computeShader, "_UVOffset", uvOffset);
        cmd.SetComputeVectorParam(computeShader, "_Offset", transform.position);
        cmd.SetComputeBufferParam(computeShader, transformVerticesKernelHandle, "_HardwareVertices", vertexBuffer);

        // Dispatch compute
        cmd.DispatchCompute(computeShader, transformVerticesKernelHandle,
                            dispatchResolution.x / 8, dispatchResolution.y / 8, 1);
        */
        // Set render target and draw procedurally
        cmd.SetRenderTarget(rasterOutputTexture);
        cmd.ClearRenderTarget(true, true, Color.clear);

        cmd.DrawProceduralIndirect(Matrix4x4.identity,
                                   hardwareRasterMaterial,
                                   0,
                                   MeshTopology.Triangles,
                                   argsBuffer);

        // (Optional) Insert a second fence if you need to wait on both before post-processing
        // var drawFence = cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.PixelProcessing);

        // Submit all to GPU
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();

        /*
        Graphics.SetRenderTarget(rasterOutputTexture);
        // GL.Viewport(new Rect(0, 0, rasterOutputTexture.width, rasterOutputTexture.height));
        // GL.Clear(true, true, Color.clear); // Optional clear

        Graphics.DrawProceduralIndirect(hardwareRasterMaterial,
                                        new Bounds(Vector3.zero, Vector3.one * 100),
                                        MeshTopology.Triangles,
                                        argsBuffer);

        // Reset target to avoid warnings
        Graphics.SetRenderTarget(null);
        */
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        
    }

    private void OnDestroy()
    {
        vertexBuffer.Release();
        argsBuffer.Release();
    }
}
