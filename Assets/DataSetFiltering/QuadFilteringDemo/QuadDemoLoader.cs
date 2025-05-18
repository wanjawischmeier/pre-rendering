using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class QuadDemoLoader : MonoBehaviour
{
    public enum TileSize
    {
        _TILE_SIZE_4 = 4,
        _TILE_SIZE_8 = 8,
        _TILE_SIZE_16 = 16,
        _TILE_SIZE_32 = 32
    }

    public enum TileCapacity
    {
        _MAX_VERTS_PER_TILE_64 = 64,
        _MAX_VERTS_PER_TILE_128 = 128,
        _MAX_VERTS_PER_TILE_256 = 256,
        _MAX_VERTS_PER_TILE_512 = 512
    }

    public ComputeShader computeShader;
    public Material softwareRasterDebug, hardwareRasterDebug, hardwareRasterMaterial, postProcessingPass, tileOccupancyDebug0, tileOccupancyDebug1, vertexLookupDebug;
    public Texture2D inputTexture;
    public RenderTexture vertexBuffer, softwareRasterizedDepth, hardwareRasterizedDepth, softwareRasterizerTileCounters0, softwareRasterizerTileCounters1;
    public RenderTexture softwareRasterizedDebugTexture, hardwareRasterizedDebugTexture;
    public float nearClip, farClip;
    public Vector2Int rescaledInputResolution, dispatchResolution, renderTargetResolution;
    public bool autoResolution = true;
    public bool blitToScreen = true;
    public bool debugWireframe = false;
    public uint[] triangleCounts;
    public int maxHardwareRasterizedTrianglesPerBatch = 131072;
    public TileSize tileSize = TileSize._TILE_SIZE_8;
    public TileCapacity tileCapacity = TileCapacity._MAX_VERTS_PER_TILE_256;

    private int transformVerticesKernelHandle, binQuadsKernel, rasterizeBinnedQuadsKernelHandle;
    private ComputeBuffer[] hardwareRasterizerQuadBuffers, hardwareRasterizerArgsBuffers;
    private ComputeBuffer softwareRasterizerVertexBuffer0, softwareRasterizerVertexBuffer1;
    private Dictionary<Material, LocalKeyword> debugWireframeKeywords;
    private LocalKeyword debugWireframeComputeKeyword;

    const int vertexBufferCount = 4;

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

        hardwareRasterizedDepth = new RenderTexture(renderTargetResolution.x, renderTargetResolution.y, 24, RenderTextureFormat.Depth);
        hardwareRasterizedDepth.Create();

        hardwareRasterizedDebugTexture = new RenderTexture(renderTargetResolution.x, renderTargetResolution.y, 0, RenderTextureFormat.ARGB32);
        hardwareRasterizedDebugTexture.Create();

        softwareRasterizedDebugTexture = new RenderTexture(hardwareRasterizedDebugTexture);
        softwareRasterizedDebugTexture.enableRandomWrite = true;
        softwareRasterizedDebugTexture.Create();

        vertexBuffer = new RenderTexture(inputTexture.width, inputTexture.height, 0, RenderTextureFormat.RGInt);
        vertexBuffer.enableRandomWrite = true;
        vertexBuffer.Create();

        int tileCountX = Mathf.CeilToInt((float)renderTargetResolution.x / (int)tileSize);
        int tileCountY = Mathf.CeilToInt((float)renderTargetResolution.y / (int)tileSize);
        softwareRasterizerTileCounters0 = new RenderTexture(tileCountX, tileCountY, 0, RenderTextureFormat.RInt);
        softwareRasterizerTileCounters0.enableRandomWrite = true;
        softwareRasterizerTileCounters1 = new RenderTexture(softwareRasterizerTileCounters0);
        softwareRasterizerTileCounters0.Create();
        softwareRasterizerTileCounters1.Create();

        triangleCounts = new uint[vertexBufferCount + 1]; // Last entry is total count
        hardwareRasterizerQuadBuffers = new ComputeBuffer[vertexBufferCount];
        hardwareRasterizerArgsBuffers = new ComputeBuffer[vertexBufferCount];
        for (int i = 0; i < vertexBufferCount; i++)
        {
            hardwareRasterizerQuadBuffers[i] = new ComputeBuffer(maxHardwareRasterizedTrianglesPerBatch, 9 * sizeof(float), ComputeBufferType.Append);
            hardwareRasterizerArgsBuffers[i] = new ComputeBuffer(1, 4 * sizeof(int), ComputeBufferType.IndirectArguments);
        }

        softwareRasterizerVertexBuffer0 = new ComputeBuffer((int)tileCapacity * tileCountX * tileCountY, 3 * sizeof(uint), ComputeBufferType.Structured);
        softwareRasterizerVertexBuffer1 = new ComputeBuffer((int)tileCapacity * tileCountX * tileCountY, 3 * sizeof(uint), ComputeBufferType.Structured);
        int softwareRasterizerVertexBufferSizeMb = Mathf.CeilToInt(softwareRasterizerVertexBuffer0.count * softwareRasterizerVertexBuffer0.stride * 2 / (1024 * 1024));
        Debug.Log($"Software Rasterizer Vertex Buffer Size: {softwareRasterizerVertexBufferSizeMb} MB");

        debugWireframeKeywords = new Dictionary<Material, LocalKeyword>()
        {
            { hardwareRasterMaterial, new LocalKeyword(hardwareRasterMaterial.shader, "_DEBUG_WIREFRAME") },
            { postProcessingPass, new LocalKeyword(postProcessingPass.shader, "_DEBUG_WIREFRAME") }
        };
        debugWireframeComputeKeyword = new LocalKeyword(computeShader, "_DEBUG_WIREFRAME");
        foreach (var tileSizeEnum in System.Enum.GetValues(typeof(TileSize)))
        {
            if (tileSizeEnum.Equals(tileSize))
            {
                computeShader.SetKeyword(new LocalKeyword(computeShader, tileSizeEnum.ToString()), true);
            }
            else
            {
                computeShader.SetKeyword(new LocalKeyword(computeShader, tileSizeEnum.ToString()), false);
            }
        }
        foreach (var tileCapacityEnum in System.Enum.GetValues(typeof(TileCapacity)))
        {
            if (tileCapacityEnum.Equals(tileCapacity))
            {
                computeShader.SetKeyword(new LocalKeyword(computeShader, tileCapacityEnum.ToString()), true);
            }
            else
            {
                computeShader.SetKeyword(new LocalKeyword(computeShader, tileCapacityEnum.ToString()), false);
            }
        }

        transformVerticesKernelHandle = computeShader.FindKernel("TransformVertices");
        binQuadsKernel = computeShader.FindKernel("BinQuads");
        rasterizeBinnedQuadsKernelHandle = computeShader.FindKernel("RasterizeTileBinnedQuads");

        Vector2 inputResolution = new Vector2(inputTexture.width, inputTexture.height);
        Vector2 outputResolution = new Vector2(renderTargetResolution.x, renderTargetResolution.y);

        computeShader.SetVector("_InputResolution", inputResolution);
        computeShader.SetVector("_OutputResolution", outputResolution);

        computeShader.SetTexture(transformVerticesKernelHandle, "_InputColorBuffer", inputTexture);
        computeShader.SetTexture(transformVerticesKernelHandle, "RW_VertexBuffer", vertexBuffer);

        computeShader.SetTexture(binQuadsKernel, "RW_VertexBuffer", vertexBuffer);
        computeShader.SetTexture(binQuadsKernel, "RW_DepthBuffer_SW", softwareRasterizedDepth);
        computeShader.SetTexture(binQuadsKernel, "RW_DebugBuffer_SW", softwareRasterizedDebugTexture);
        computeShader.SetTexture(binQuadsKernel, "_TileCounters_SW0", softwareRasterizerTileCounters0);
        computeShader.SetTexture(binQuadsKernel, "_TileCounters_SW1", softwareRasterizerTileCounters1);
        computeShader.SetBuffer(binQuadsKernel, "_QuadIndexBuffer_SW0", softwareRasterizerVertexBuffer0);
        computeShader.SetBuffer(binQuadsKernel, "_QuadIndexBuffer_SW1", softwareRasterizerVertexBuffer1);

        computeShader.SetTexture(rasterizeBinnedQuadsKernelHandle, "RW_VertexBuffer", vertexBuffer);
        computeShader.SetTexture(rasterizeBinnedQuadsKernelHandle, "RW_DepthBuffer_SW", softwareRasterizedDepth);
        computeShader.SetTexture(rasterizeBinnedQuadsKernelHandle, "RW_DebugBuffer_SW", softwareRasterizedDebugTexture);

        postProcessingPass.SetVector("_OutputResolution", outputResolution);
        postProcessingPass.SetTexture("_DepthBuffer_SW", softwareRasterizedDepth);
        postProcessingPass.SetTexture("_DebugBuffer_SW", softwareRasterizedDebugTexture);
        postProcessingPass.SetTexture("_DepthBuffer_HW", hardwareRasterizedDepth);
        postProcessingPass.SetTexture("_DebugBuffer_HW", hardwareRasterizedDebugTexture);

        // Debugging materials
        if (softwareRasterDebug != null)
        {
            softwareRasterDebug.SetVector("_OutputResolution", outputResolution);
            softwareRasterDebug.SetTexture("_InputDepthBuffer", softwareRasterizedDepth);
            softwareRasterDebug.SetTexture("_InputDebugBuffer", softwareRasterizedDebugTexture);

            debugWireframeKeywords.Add(softwareRasterDebug, new LocalKeyword(softwareRasterDebug.shader, "_DEBUG_WIREFRAME"));
        }

        if (hardwareRasterDebug != null)
        {
            hardwareRasterDebug.SetVector("_OutputResolution", outputResolution);
            hardwareRasterDebug.SetTexture("_InputDepthBuffer", hardwareRasterizedDepth);
            hardwareRasterDebug.SetTexture("_InputDebugBuffer", hardwareRasterizedDebugTexture);

            debugWireframeKeywords.Add(hardwareRasterDebug, new LocalKeyword(hardwareRasterDebug.shader, "_DEBUG_WIREFRAME"));
        }

        if (tileOccupancyDebug0 != null && tileOccupancyDebug1 != null)
        {
            tileOccupancyDebug0.SetInt("_TileSize", (int)tileSize);
            tileOccupancyDebug0.SetInt("_MaxVertsPerTile", (int)tileCapacity);
            tileOccupancyDebug0.SetVector("_OutputResolution", outputResolution);
            tileOccupancyDebug0.SetTexture("_TileCounters", softwareRasterizerTileCounters0);

            tileOccupancyDebug1.SetInt("_TileSize", (int)tileSize);
            tileOccupancyDebug1.SetInt("_MaxVertsPerTile", (int)tileCapacity);
            tileOccupancyDebug1.SetVector("_OutputResolution", outputResolution);
            tileOccupancyDebug1.SetTexture("_TileCounters", softwareRasterizerTileCounters1);
        }

        if (vertexLookupDebug != null)
        {
            vertexLookupDebug.SetTexture("_VertexBuffer", vertexBuffer);
            vertexLookupDebug.SetVector("_InputResolution", inputResolution);
            vertexLookupDebug.SetVector("_OutputResolution", outputResolution);
        }
    }

    private void Update()
    {
        // TODO: Check which GL.Clear's are actually needed
        RenderTexture rt = RenderTexture.active;
        if (debugWireframe)
        {
            RenderTexture.active = softwareRasterizedDebugTexture;
            GL.Clear(true, true, Color.clear);
        }
        else
        {
            RenderTexture.active = softwareRasterizedDepth;
            GL.Clear(true, true, new Color(int.MaxValue, 0, 0));
        }
        RenderTexture.active = softwareRasterizerTileCounters0;
        GL.Clear(true, true, new Color(0, 0, 0));
        RenderTexture.active = softwareRasterizerTileCounters1;
        GL.Clear(true, true, new Color(0, 0, 0));
        RenderTexture.active = rt;

        Matrix4x4 trs = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Matrix4x4 viewMatrix = Camera.main.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, true);
        Matrix4x4 viewProjMatrix = projMatrix * viewMatrix;
        computeShader.SetMatrix("_CameraViewProj", viewProjMatrix);

        computeShader.SetFloat("_MapNearClip", nearClip);
        computeShader.SetFloat("_MapFarClip", farClip);

        for (int i = 0; i < vertexBufferCount; i++)
        {
            hardwareRasterizerQuadBuffers[i].SetCounterValue(0); // Reset append buffer
            computeShader.SetBuffer(binQuadsKernel, $"_QuadIndexBuffer_HW{i}", hardwareRasterizerQuadBuffers[i]);
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

        foreach (var keyword in debugWireframeKeywords)
        {
            keyword.Key.SetKeyword(keyword.Value, debugWireframe);
        }
        computeShader.SetKeyword(debugWireframeComputeKeyword, debugWireframe);

        var cmd = new CommandBuffer();
        cmd.name = "Draw Hardware & Software Raster Batches";

        cmd.SetComputeTextureParam(computeShader, rasterizeBinnedQuadsKernelHandle, "_TileCounters_SW", softwareRasterizerTileCounters0);
        cmd.SetComputeBufferParam(computeShader, rasterizeBinnedQuadsKernelHandle, "_QuadIndexBuffer_SW", softwareRasterizerVertexBuffer0);
        cmd.DispatchCompute(computeShader, rasterizeBinnedQuadsKernelHandle, renderTargetResolution.x / (int)tileSize, renderTargetResolution.y / (int)tileSize, 1);

        cmd.SetComputeTextureParam(computeShader, rasterizeBinnedQuadsKernelHandle, "_TileCounters_SW", softwareRasterizerTileCounters1);
        cmd.SetComputeBufferParam(computeShader, rasterizeBinnedQuadsKernelHandle, "_QuadIndexBuffer_SW", softwareRasterizerVertexBuffer1);
        cmd.DispatchCompute(computeShader, rasterizeBinnedQuadsKernelHandle, renderTargetResolution.x / (int)tileSize, renderTargetResolution.y / (int)tileSize, 1);


        // Set render target and draw procedurally
        cmd.SetRenderTarget(debugWireframe ? hardwareRasterizedDebugTexture : hardwareRasterizedDepth);
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
            props.SetBuffer("_QuadIndexBuffer", hardwareRasterizerQuadBuffers[i]);
            props.SetTexture("_VertexBuffer", vertexBuffer);

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
        if (blitToScreen)
        {
            Graphics.Blit(null, destination, postProcessingPass);
        }
    }

    private void OnDestroy()
    {
        // Release all resources
        softwareRasterizedDepth.Release();
        softwareRasterizerTileCounters0.Release();
        softwareRasterizerTileCounters1.Release();
        softwareRasterizerVertexBuffer0.Release();
        softwareRasterizerVertexBuffer1.Release();
        hardwareRasterizedDebugTexture.Release();

        for (int i = 0; i < vertexBufferCount; i++)
        {
            hardwareRasterizerQuadBuffers[i].Release();
            hardwareRasterizerArgsBuffers[i].Release();
        }
    }
}
