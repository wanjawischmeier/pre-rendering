using System;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class RenderManager : MonoBehaviour
{
    [Serializable]
    public struct DebugPos
    {
        public Vector4[] positions;
    }

    public MapScriptableObject map;
    public Material cubemapScreenBlitMaterial;
    public ComputeShader computeShader, cubemapRenderShader;
    public Vector2Int fullResolution, downsampledResolution;
    public Vector3Int[] resolutions;    // z stores max number of threads at that resolution
    public int cubemapSlice = 0;
    public bool dispatchInput, dispatchBackBuffer, dispatchDownsampled;
    public RenderTexture[] fbBufferFullRes, fbBufferDownsampled;
    public RenderTexture input;
    public DebugPos[] debugPos;
    public float off;
    public Vector3 offs;
    public Material debugTextureMaterial;

    Vector3 previousPosition = Vector3.one;
    int iterativeTransformKernelId;
    uint threadsX, threadsY;
    PingPongBuffer pingPongBufferFullRes, pingPongBufferDownsampled, pingPongDepthBufferIterative;
    PingPongBuffer pingPongDepthBufferFullRes, pingPongDepthBufferDownsampledLayer0, pingPongDepthBufferDownsampledLayer1;
    CubemapRenderer testCubemapRenderer, cubemap0, cubemap1;


    public struct CubemapComputeShaderData
    {
        public ComputeShader computeShader;
        public int renderKernelId;
        public uint renderThreadsX, renderThreadsY;
    }
    public static CubemapComputeShaderData cubemapComputeShaderData;

    const int CUBEMAP_FACE_COUNT = 6;

    private void Start()
    {
        cubemapComputeShaderData = new CubemapComputeShaderData() { computeShader = cubemapRenderShader };
        cubemapComputeShaderData.renderKernelId = cubemapRenderShader.FindKernel("RenderCubemap");
        cubemapRenderShader.GetKernelThreadGroupSizes(cubemapComputeShaderData.renderKernelId,
                                                      out cubemapComputeShaderData.renderThreadsX,
                                                      out cubemapComputeShaderData.renderThreadsY,
                                                      out uint _);

        cubemapRenderShader.SetFloat("_MapNearClip", map.nearClipPlane);
        cubemapRenderShader.SetFloat("_MapFarClip", map.farClipPlane);
        cubemapRenderShader.SetMatrixArray("_OrientationMatricies", CubeMapConversion.orientationMatricies);

        CubemapRenderer.RendererConfig rendererConfig = new CubemapRenderer.RendererConfig()
        {
            fullResolution = fullResolution.ToResolution(),
            downsampledResolution = downsampledResolution.ToResolution(),
        };
        testCubemapRenderer = new CubemapRenderer(rendererConfig);
        cubemap0 = new CubemapRenderer(rendererConfig);
        cubemap1 = new CubemapRenderer(rendererConfig);


        iterativeTransformKernelId = computeShader.FindKernel("IterativeTransform");
        computeShader.GetKernelThreadGroupSizes(iterativeTransformKernelId, out threadsX, out threadsY, out uint _);
        
        pingPongBufferFullRes = new PingPongBuffer(fullResolution.ToResolution(), CUBEMAP_FACE_COUNT, RenderTextureFormat.ARGBHalf);
        fbBufferFullRes = pingPongBufferFullRes.Textures;

        pingPongDepthBufferFullRes = new PingPongBuffer(fullResolution.ToResolution(), CUBEMAP_FACE_COUNT, RenderTextureFormat.RInt);
        pingPongDepthBufferIterative = new PingPongBuffer(fullResolution.ToResolution(), CUBEMAP_FACE_COUNT, RenderTextureFormat.RInt);    // TODO: normal rt would suffice, back buffer is not used
        pingPongDepthBufferDownsampledLayer0 = new PingPongBuffer(downsampledResolution.ToResolution(), CUBEMAP_FACE_COUNT, RenderTextureFormat.RInt);
        pingPongDepthBufferDownsampledLayer1 = new PingPongBuffer(downsampledResolution.ToResolution(), CUBEMAP_FACE_COUNT, RenderTextureFormat.RInt);

        pingPongBufferDownsampled = new PingPongBuffer(downsampledResolution.ToResolution(), CUBEMAP_FACE_COUNT, RenderTextureFormat.ARGBHalf);
        fbBufferDownsampled = pingPongBufferDownsampled.Textures;

        input = new RenderTexture(map.inputImages[0].width, map.inputImages[0].height, 0);
        input.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        input.format = RenderTextureFormat.ARGBHalf;
        input.enableRandomWrite = true;
        input.volumeDepth = map.inputImages.Length; // expected to be multiple of 6

        // copy input textures to tex2darray
        for (int cubemapIndex = 0; cubemapIndex < input.volumeDepth; cubemapIndex++)
        {
            Graphics.Blit(map.inputImages[cubemapIndex], input, 0, cubemapIndex);
        }

        computeShader.SetFloat("NCLIP", map.nearClipPlane);
        computeShader.SetFloat("FCLIP", map.farClipPlane);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetVector("TARGET_RESOLUTION_FULL", new Vector2(fullResolution.x, fullResolution.y));
        computeShader.SetVector("TARGET_RESOLUTION_DOWNSAMPLED", new Vector2(downsampledResolution.x, downsampledResolution.y));
        computeShader.SetVector("_ScreenSize", new Vector2(Screen.width, Screen.height));

        // to prevent null reference exception, although these variables are properly set later
        computeShader.SetTexture(iterativeTransformKernelId, "InputSecondary", pingPongBufferDownsampled.Back);
        computeShader.SetTexture(iterativeTransformKernelId, "FrontDepthBufferDownsampledSecondary", pingPongDepthBufferDownsampledLayer1.Front);
        computeShader.SetTexture(iterativeTransformKernelId, "BackDepthBufferFullRes", pingPongDepthBufferFullRes.Back);

        computeShader.SetVectorArray("CUBE_POSITIONS", map.cubemapPositions);
        computeShader.SetMatrixArray("_OrientationMatricies", CubeMapConversion.orientationMatricies);
        computeShader.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);

        cubemapScreenBlitMaterial.SetFloat("NCLIP", map.nearClipPlane);
        cubemapScreenBlitMaterial.SetFloat("FCLIP", map.farClipPlane);
        cubemapScreenBlitMaterial.SetVector("TARGET_RESOLUTION_DOWNSAMPLED", new Vector2(downsampledResolution.x, downsampledResolution.y));
        cubemapScreenBlitMaterial.SetMatrixArray("_OrientationMatricies", CubeMapConversion.orientationMatricies);
        cubemapScreenBlitMaterial.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);
        cubemapScreenBlitMaterial.SetTexture("_Input", input);

        for (int i = 0; i < 3; i++) // why thrice?
        {
            cubemap0.Render(input, new Vector3(0, 0, 0), fullResolution.ToResolution());
            cubemap1.Render(input, new Vector3(1, 0, 0), fullResolution.ToResolution());
        }

        Update();
        previousPosition = Vector3.one;
        Update();
        previousPosition = Vector3.one; // why tf is this necessary?
    }

    private void Update()
    {
        Matrix4x4 viewToWorldMatrix = Camera.main.cameraToWorldMatrix;
        Matrix4x4 invProjMatrix = Camera.main.projectionMatrix.inverse;
        cubemapScreenBlitMaterial.SetMatrix("_InvProjectionMatrix", invProjMatrix);
        cubemapScreenBlitMaterial.SetMatrix("_ViewToWorldMatrix", viewToWorldMatrix);

        if (transform.position.Equals(previousPosition))
        {
            return;
        }

        // swap front and back buffers
        pingPongBufferFullRes.Swap(PingPongBuffer.ClearMode.ColorBuffer);
        pingPongBufferDownsampled.Swap();
        pingPongDepthBufferFullRes.Swap(PingPongBuffer.ClearMode.DepthBuffer);              // clears to int.MaxValue
        pingPongDepthBufferIterative.Swap(PingPongBuffer.ClearMode.DepthBuffer);
        pingPongDepthBufferDownsampledLayer0.Swap(PingPongBuffer.ClearMode.DepthBuffer);
        pingPongDepthBufferDownsampledLayer1.Swap(PingPongBuffer.ClearMode.ColorBuffer);    // clears to 0

        computeShader.SetBool("ONLY_DISPATCH_DOWNSAMPLED", dispatchDownsampled);
        computeShader.SetFloat("CAM_FCLIP", Camera.main.farClipPlane);
        computeShader.SetFloat("OFF", off);
        computeShader.SetVectorArray("CUBE_POSITIONS", map.cubemapPositions);
        computeShader.SetTexture(iterativeTransformKernelId, "FrontBufferFullRes", pingPongBufferFullRes.Front);
        computeShader.SetTexture(iterativeTransformKernelId, "FrontBufferDownsampled", pingPongBufferDownsampled.Front);
        computeShader.SetTexture(iterativeTransformKernelId, "FrontDepthBufferDownsampledPrimary", pingPongDepthBufferDownsampledLayer0.Front);
        computeShader.SetTexture(iterativeTransformKernelId, "FrontDepthBufferDownsampledSecondary", pingPongDepthBufferDownsampledLayer1.Front);
        computeShader.SetTexture(iterativeTransformKernelId, "BackDepthBufferFullRes", pingPongDepthBufferFullRes.Back);
        computeShader.SetTexture(iterativeTransformKernelId, "BackDepthBufferDownsampledPrimary", pingPongDepthBufferDownsampledLayer0.Back);

        if (dispatchBackBuffer)
        {
            DispatchBackBufferComputeShader();
        }
        if (dispatchInput)
        {
            DispatchInputComputeShader();
        }

        // update screen blit material after textures have been updated
        cubemapScreenBlitMaterial.SetFloat("CAM_FCLIP", Camera.main.farClipPlane);
        cubemapScreenBlitMaterial.SetVector("_PlayerPosition", transform.position);
        cubemapScreenBlitMaterial.SetTexture("_FrontBufferFullRes", pingPongBufferFullRes.Front);
        cubemapScreenBlitMaterial.SetTexture("_FrontBufferDownsampled", pingPongBufferDownsampled.Front);
        cubemapScreenBlitMaterial.SetTexture("_FrontDepthBufferFullRes", pingPongDepthBufferFullRes.Front);
        cubemapScreenBlitMaterial.SetTexture("_FrontDepthBufferIterative", pingPongDepthBufferIterative.Front);
        cubemapScreenBlitMaterial.SetTexture("_FrontDepthBufferDownsampledLayer0", pingPongDepthBufferDownsampledLayer0.Front);
        cubemapScreenBlitMaterial.SetTexture("_FrontDepthBufferDownsampledLayer1", pingPongDepthBufferDownsampledLayer1.Front);


        // temp substitution for ping pong buffer
        RenderTexture previousFrameDownsampled = RenderTexture.GetTemporary(testCubemapRenderer.downsampledRenderBuffer.descriptor);
        Graphics.CopyTexture(testCubemapRenderer.downsampledRenderBuffer, previousFrameDownsampled);

        /*
        RenderTexture rt = RenderTexture.active;
        for (int faceIndex = 0; faceIndex < CUBEMAP_FACE_COUNT; faceIndex++)
        {
            Graphics.SetRenderTarget(testCubemapRenderer.downsampledRenderBuffer, 0, CubemapFace.Unknown, faceIndex);
            GL.Clear(true, true, new Color(int.MaxValue, 0, 0));
        }
        RenderTexture.active = rt;
        */
        testCubemapRenderer.Render(input, -transform.position, fullResolution.ToResolution());

        RenderTexture depthMaskTexture = RenderTexture.GetTemporary(testCubemapRenderer.downsampledRenderBuffer.descriptor);
        Graphics.CopyTexture(testCubemapRenderer.downsampledRenderBuffer, depthMaskTexture);

        testCubemapRenderer.Render(previousFrameDownsampled, previousPosition - transform.position,
                                   downsampledResolution.ToResolution(), clearBuffer: false,
                                   dispatchTargetMode: CubemapRenderer.DispatchTargetMode.Downsampled,
                                   depthMask: depthMaskTexture);
        
        // cubemap0.Render(testCubemapRenderer.downsampledRenderBuffer, transform.position, clearBuffer: false);
        debugTextureMaterial.SetTexture("_Input", testCubemapRenderer.downsampledRenderBuffer);
        cubemapScreenBlitMaterial.SetTexture("_FrontDepthBufferFullRes", testCubemapRenderer.fullResRenderBuffer);
        cubemapScreenBlitMaterial.SetTexture("_FrontDepthBufferDownsampled", testCubemapRenderer.downsampledRenderBuffer);

        previousFrameDownsampled.Release();
        depthMaskTexture.Release();

        previousPosition = transform.position;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, cubemapScreenBlitMaterial);
    }

    private void DispatchBackBufferComputeShader()
    {
        int dispatchWidth = pingPongDepthBufferFullRes.Back.width;
        int dispatchHeight = pingPongDepthBufferFullRes.Back.height;
        int threadGroupsX = Mathf.CeilToInt(dispatchWidth / (int)threadsX);
        int threadGroupsY = Mathf.CeilToInt(dispatchHeight / (int)threadsY);
        offs = transform.position - previousPosition;
        computeShader.SetBool("IS_DISPATCHING_ITERATIVELY", true);
        computeShader.SetVector("OFFSET", transform.position - previousPosition);
        computeShader.SetVector("DISPATCH_RESOLUTION", new Vector2(dispatchWidth, dispatchHeight));
        computeShader.SetTexture(iterativeTransformKernelId, "FrontDepthBufferFullRes", pingPongDepthBufferIterative.Front);

        computeShader.Dispatch(iterativeTransformKernelId, threadGroupsX, threadGroupsY, 6);
    }

    private void DispatchInputComputeShader()
    {
        // Set global shader parameters
        computeShader.SetBool("IS_DISPATCHING_ITERATIVELY", false);
        computeShader.SetVector("OFFSET", transform.position);
        computeShader.SetTexture(iterativeTransformKernelId, "Input", input);
        computeShader.SetTexture(iterativeTransformKernelId, "FrontDepthBufferFullRes", pingPongDepthBufferFullRes.Front);

        cubemapScreenBlitMaterial.SetVector("_PlayerPosition", transform.position);

        // Sort cubemap positions by distance (closest first), then reverse to get furthest first
        Vector4[] sortedPositions = map.cubemapPositions
            .Select((pos, index) => new Vector4(pos.x, pos.y, pos.z, index)) // Store original index in 'w'
            .OrderBy(v => Vector3.Distance((Vector3)v, transform.position))
            .Reverse() // Reverse to get furthest first
            .ToArray();

        // pass sorted positions to compute shader, closest first
        cubemapScreenBlitMaterial.SetVectorArray("SORTED_CUBE_POSITIONS", sortedPositions.Reverse().ToArray());

        debugPos = new DebugPos[resolutions.Length];

        int remainingCount = sortedPositions.Length;

        // Iterate over resolutions in order, but take from the back of the array
        for (int i = 0; i < resolutions.Length; i++)
        {
            Vector3Int resolution = resolutions[i];
            if (resolution.x == 0 || resolution.y == 0 || resolution.z == 0) continue;  // Skip if resolution is zero

            int maxThreads = resolution.z;
            int count = Mathf.Min(maxThreads, remainingCount);

            if (count == 0) break;  // Stop if no positions remain

            // Take the last 'count' elements (so we start with the furthest in the current batch)
            Vector4[] selectedPositions = sortedPositions.Skip(remainingCount - count).Take(count).ToArray();
            debugPos[i] = new DebugPos() { positions = selectedPositions };
            computeShader.SetVectorArray("CUBE_POSITIONS", selectedPositions);  // TODO: ensure const size of CUBE_POSITIONS
            computeShader.SetVector("DISPATCH_RESOLUTION", new Vector2(resolution.x, resolution.y));

            // Compute dispatch size
            int threadGroupsX = Mathf.CeilToInt(resolution.x / (int)threadsX);
            int threadGroupsY = Mathf.CeilToInt(resolution.y / (int)threadsY);

            // Dispatch compute shader
            computeShader.Dispatch(iterativeTransformKernelId, threadGroupsX, threadGroupsY, selectedPositions.Length * 6);

            // Reduce remaining positions
            remainingCount -= count;
        }

    }


    public void SetMap(MapScriptableObject map)
    {
        this.map = map;
        previousPosition = Vector3.one;
        Start();
        Update();

        if (TryGetComponent(out FlyMovementController movementController))
        {
            movementController.TryApplyMapSpeedMultiplier();
        }
    }

    public void UpdateCubemap0()
    {
        cubemap0.Render(testCubemapRenderer.downsampledRenderBuffer, transform.position, fullResolution.ToResolution(), clearBuffer: false);
        previousPosition = Vector3.one;
        Update();

        Debug.Log("Updated cubemap 0");
    }
}

public static class Utility
{
    public static Resolution ToResolution(this Vector2Int vector)
    {
        return new Resolution() { width = vector.x, height = vector.y };
    }
}