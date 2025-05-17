using UnityEngine;
using UnityEngine.Rendering;

public class QuadDemoLoader : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material softwareRasterDebug, hardwareRasterDebug, hardwareRasterMaterial, tileOccupancyDebug, vertexLookupDebug;
    public Texture2D inputTexture;
    public RenderTexture vertexLookup, softwareRasterizedDepth, hardwareRasterizedDepth, softwareRasterizerTileCounters;
    public float nearClip, farClip;
    public Vector2Int rescaledInputResolution, dispatchResolution, renderTargetResolution;
    public bool autoResolution = true;
    public uint[] triangleCounts;
    public int maxHardwareRasterizedTrianglesPerBatch = 131072;
    private ComputeBuffer[] hardwareRasterizerQuadBuffers, hardwareRasterizerArgsBuffers;
    private ComputeBuffer softwareRasterizerVertexBuffer;

    private int transformVerticesKernelHandle, binQuadsKernel, rasterizeBinnedQuadsKernelHandle;
    const int vertexBufferCount = 4;
    const int swTileSize = 32;
    const int swMaxVertsPerTile = 256; // 16x16 tile

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

        if (autoResolution)
        {
            dispatchResolution = new Vector2Int(inputTexture.width, inputTexture.height);
        }

        softwareRasterizedDepth = new RenderTexture(renderTargetResolution.x, renderTargetResolution.y, 0, RenderTextureFormat.RInt);
        softwareRasterizedDepth.enableRandomWrite = true;
        softwareRasterizedDepth.Create();

        vertexLookup = new RenderTexture(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.RGInt);
        vertexLookup.enableRandomWrite = true;
        vertexLookup.Create();

        int tileCountX = Mathf.CeilToInt((float)renderTargetResolution.x / swTileSize);
        int tileCountY = Mathf.CeilToInt((float)renderTargetResolution.y / swTileSize);
        softwareRasterizerTileCounters = new RenderTexture(tileCountX, tileCountY, 0, RenderTextureFormat.RInt);
        softwareRasterizerTileCounters.enableRandomWrite = true;
        softwareRasterizerTileCounters.Create();

        hardwareRasterizedDepth = new RenderTexture(renderTargetResolution.x, renderTargetResolution.y, 0, RenderTextureFormat.ARGB32);
        hardwareRasterizedDepth.Create();
        hardwareRasterDebug.mainTexture = hardwareRasterizedDepth;

        triangleCounts = new uint[vertexBufferCount + 1]; // Last entry is total count
        hardwareRasterizerQuadBuffers = new ComputeBuffer[vertexBufferCount];
        hardwareRasterizerArgsBuffers = new ComputeBuffer[vertexBufferCount];
        for (int i = 0; i < vertexBufferCount; i++)
        {
            hardwareRasterizerQuadBuffers[i] = new ComputeBuffer(maxHardwareRasterizedTrianglesPerBatch, 9 * sizeof(float), ComputeBufferType.Append);
            hardwareRasterizerArgsBuffers[i] = new ComputeBuffer(1, 4 * sizeof(int), ComputeBufferType.IndirectArguments);
        }

        softwareRasterizerVertexBuffer = new ComputeBuffer(swMaxVertsPerTile * tileCountX * tileCountY, 6 * sizeof(float), ComputeBufferType.Structured);
        Debug.Log($"Software Rasterizer Vertex Buffer Size: {swMaxVertsPerTile * tileCountX * tileCountY * 6 * sizeof(float) / (1024f * 1024f)} MB");

        transformVerticesKernelHandle = computeShader.FindKernel("TransformVertices");
        binQuadsKernel = computeShader.FindKernel("BinQuads");
        rasterizeBinnedQuadsKernelHandle = computeShader.FindKernel("RasterizeTileBinnedQuads");
        computeShader.SetVector("_InputResolution", new Vector2(inputTexture.width, inputTexture.height));
        // computeShader.SetVector("_DispatchResolution", new Vector2(dispatchResolution.x, dispatchResolution.y));
        computeShader.SetVector("_OutputResolution", new Vector2(renderTargetResolution.x, renderTargetResolution.y));
        // computeShader.SetMatrixArray("_OrientationMatricies", CubeMapConversion.orientationMatricies);

        computeShader.SetTexture(transformVerticesKernelHandle, "_InputColorBuffer", inputTexture);
        computeShader.SetTexture(transformVerticesKernelHandle, "RW_VertexLookup", vertexLookup);
        computeShader.SetTexture(binQuadsKernel, "RW_VertexLookup", vertexLookup);
        // computeShader.SetBuffer(transformVerticesKernelHandle, "g_SWQuads", softwareRasterizerVertexBuffer);
        // computeShader.SetBuffer(rasterizeBinnedQuadsKernelHandle, "g_SWQuads", softwareRasterizerVertexBuffer);
        // computeShader.SetTexture(transformVerticesKernelHandle, "g_SWTileCounters", softwareRasterizerTileCounters);
        // computeShader.SetTexture(rasterizeBinnedQuadsKernelHandle, "g_SWTileCounters", softwareRasterizerTileCounters);
        // computeShader.SetTexture(transformVerticesKernelHandle, "RW_SWDepthBuffer", softwareRasterizedDepth);
        // computeShader.SetTexture(rasterizeBinnedQuadsKernelHandle, "RW_SWDepthBuffer", softwareRasterizedDepth);

        softwareRasterDebug.SetVector("_InputResolution", new Vector2(renderTargetResolution.x, renderTargetResolution.y));
        softwareRasterDebug.SetTexture("_InputDepthBuffer", softwareRasterizedDepth);
        tileOccupancyDebug.SetTexture("_TileCounts", softwareRasterizerTileCounters);
        vertexLookupDebug.SetTexture("_VertexLookup", vertexLookup);
        vertexLookupDebug.SetVector("_InputResolution", new Vector2(inputTexture.width, inputTexture.height));
        vertexLookupDebug.SetVector("_OutputResolution", new Vector2(renderTargetResolution.x, renderTargetResolution.y));
    }

    private void Update()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = softwareRasterizedDepth;
        GL.Clear(true, true, new Color(int.MaxValue, 0, 0));
        RenderTexture.active = softwareRasterizerTileCounters;
        GL.Clear(true, true, new Color(0, 0, 0));
        RenderTexture.active = rt;

        Matrix4x4 trs = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, true);
        Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
        computeShader.SetMatrix("_CameraViewProj", viewProjMatrix);

        computeShader.SetFloat("_MapNearClip", nearClip);
        computeShader.SetFloat("_MapFarClip", farClip);
        computeShader.SetVector("_Offset", transform.position);

        for (int i = 0; i < vertexBufferCount; i++)
        {
            hardwareRasterizerQuadBuffers[i].SetCounterValue(0); // Reset append buffer
            computeShader.SetBuffer(binQuadsKernel, $"_HWQuads{i}", hardwareRasterizerQuadBuffers[i]);
        }

        computeShader.Dispatch(transformVerticesKernelHandle, inputTexture.width / 8, inputTexture.height / 8, 1);
        computeShader.Dispatch(binQuadsKernel, inputTexture.width / 8, inputTexture.height / 8, 1);

        uint totalTriangleCount = 0;
        uint[] args = new uint[4];
        for (int i = 0; i < vertexBufferCount; i++)
        {
            // Copy count from append buffer to args[0] (vertex count)
            ComputeBuffer.CopyCount(hardwareRasterizerQuadBuffers[i], hardwareRasterizerArgsBuffers[i], 0);

            hardwareRasterizerArgsBuffers[i].GetData(args);
            args[0] *= 6; // vertex count = triangle count * 3 = quad count * 6
            args[1] = 1;
            hardwareRasterizerArgsBuffers[i].SetData(args);

            triangleCounts[i] = args[0]; // For debugging
            totalTriangleCount += args[0];
        }
        triangleCounts[vertexBufferCount] = totalTriangleCount;

        var cmd = new CommandBuffer();
        cmd.name = "Draw Hardware & Software Raster Batches";

        // cmd.DispatchCompute(computeShader, rasterizeBinnedQuadsKernelHandle, renderTargetResolution.x / swTileSize, renderTargetResolution.y / swTileSize, 1);
        // cmd.DispatchCompute(computeShader, rasterizeBinnedQuadsKernelHandle, renderTargetResolution.x / swTileSize, renderTargetResolution.y / swTileSize, 1);


        // Set render target and draw procedurally
        cmd.SetRenderTarget(hardwareRasterizedDepth);
        cmd.ClearRenderTarget(true, true, Color.clear);

        // For each buffer group
        for (int i = 0; i < 4; i++)
        {
            if (triangleCounts[i] == 0)
                continue; // Skip empty buffers

            // Set hardware raster material parameters
            var props = new MaterialPropertyBlock();
            props.SetFloat("_CameraNearClip", Camera.main.nearClipPlane);
            props.SetFloat("_CameraFarClip", Camera.main.farClipPlane);
            props.SetVector("_InputResolution", new Vector2(inputTexture.width, inputTexture.height));
            props.SetVector("_OutputResolution", new Vector2(renderTargetResolution.x, renderTargetResolution.y));
            props.SetBuffer("_Quads", hardwareRasterizerQuadBuffers[i]);
            props.SetTexture("_VertexLookup", vertexLookup);

            cmd.DrawProceduralIndirect(Matrix4x4.identity,
                                   hardwareRasterMaterial,
                                   0,
                                   MeshTopology.Triangles,
                                   hardwareRasterizerArgsBuffers[i],
                                   0,
                                   props);
        }
        
        // (Optional) Insert a second fence if you need to wait on both before post-processing
        // var drawFence = cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.PixelProcessing);

        // Submit all to GPU
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        
    }

    private void OnDestroy()
    {
        // Release all resources
        softwareRasterizedDepth.Release();
        softwareRasterizerTileCounters.Release();
        hardwareRasterizedDepth.Release();

        for (int i = 0; i < vertexBufferCount; i++)
        {
            hardwareRasterizerQuadBuffers[i].Release();
            hardwareRasterizerArgsBuffers[i].Release();
        }

        softwareRasterizerVertexBuffer.Release();
    }
}
