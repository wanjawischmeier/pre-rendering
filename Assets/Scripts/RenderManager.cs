using System;
using System.Linq;
using UnityEngine;

public class RenderManager : MonoBehaviour
{
    [Serializable]
    public struct DebugPos
    {
        public Vector4[] positions;
    }

    public MapScriptableObject map;
    public Material cubemapScreenBlitMaterial;
    public ComputeShader computeShader;
    public Vector2Int downsampledResolution;
    public Vector3Int[] resolutions;    // z stores max number of threads at that resolution
    public int cubemapSlice = 0;
    public bool dispatchInput, dispatchBackBuffer, dispatchDownsampled;
    public RenderTexture[] fbBufferFullRes, fbBufferDownsampled;
    public RenderTexture input, inputDownsampled;
    public DebugPos[] debugPos;

    Vector3 previousPosition = Vector3.one;
    int iterativeTransformKernelId;
    uint threadsX, threadsY;
    PingPongBuffer pingPongBufferFullRes, pingPongBufferDownsampled;

    private void Start()
    {
        iterativeTransformKernelId = computeShader.FindKernel("IterativeTransform");
        computeShader.GetKernelThreadGroupSizes(iterativeTransformKernelId, out threadsX, out threadsY, out uint _);

        int lastValidIndex = resolutions
            .Select((value, index) => value.z == 0 ? int.MaxValue : index)
            .Min(); // the greatest valid resolution should be used for target textures
        int dispatchWidth = resolutions[lastValidIndex].x;
        int dispatchHeight = resolutions[lastValidIndex].y;

        pingPongBufferFullRes = new PingPongBuffer(dispatchWidth, dispatchHeight, RenderTextureFormat.ARGBHalf);
        fbBufferFullRes = pingPongBufferFullRes.Textures;

        pingPongBufferDownsampled = new PingPongBuffer(downsampledResolution.x, downsampledResolution.y, RenderTextureFormat.ARGBHalf);
        fbBufferDownsampled = pingPongBufferDownsampled.Textures;

        input = new RenderTexture(dispatchWidth, dispatchHeight, 0);
        input.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        input.format = RenderTextureFormat.ARGBHalf;
        input.enableRandomWrite = true;
        input.volumeDepth = map.inputImages.Length; // expected to be multiple of 6

        inputDownsampled = new RenderTexture(downsampledResolution.x, downsampledResolution.y, 0);
        inputDownsampled.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        inputDownsampled.format = RenderTextureFormat.ARGBHalf;
        inputDownsampled.enableRandomWrite = true;
        inputDownsampled.volumeDepth = map.inputImages.Length;

        // copy input textures to tex2darray
        for (int cubemapIndex = 0; cubemapIndex < input.volumeDepth; cubemapIndex++)
        {
            Graphics.Blit(map.inputImages[cubemapIndex], input, 0, cubemapIndex);
            Graphics.Blit(map.inputImages[cubemapIndex], inputDownsampled, 0, cubemapIndex);
        }

        computeShader.SetFloat("NCLIP", map.nearClipPlane);
        computeShader.SetFloat("FCLIP", map.farClipPlane);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetVector("INPUT_DOWNSAMPLED_RESOLUTION", new Vector2(inputDownsampled.width, inputDownsampled.height));
        computeShader.SetVector("TARGET_RESOLUTION", new Vector2(dispatchWidth, dispatchHeight));
        computeShader.SetVector("TARGET_DOWNSAMPLED_RESOLUTION", new Vector2(downsampledResolution.x, downsampledResolution.y));

        computeShader.SetVectorArray("CUBE_POSITIONS", map.cubemapPositions);
        computeShader.SetMatrixArray("ORIENTATION_MATRICIES", CubeMapConversion.orientationMatricies);
        computeShader.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);

        cubemapScreenBlitMaterial.SetFloat("FCLIP", map.farClipPlane);
        cubemapScreenBlitMaterial.SetVector("INPUT_DOWNSAMPLED_RESOLUTION", new Vector2(inputDownsampled.width, inputDownsampled.height));
        cubemapScreenBlitMaterial.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);
        cubemapScreenBlitMaterial.SetVectorArray("CUBE_POSITIONS", map.cubemapPositions);
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
        pingPongBufferFullRes.Swap();
        pingPongBufferDownsampled.Swap();

        computeShader.SetBool("ONLY_DISPATCH_DOWNSAMPLED", dispatchDownsampled);
        computeShader.SetVectorArray("CUBE_POSITIONS", map.cubemapPositions);
        computeShader.SetTexture(iterativeTransformKernelId, "FrontBufferFullRes", pingPongBufferFullRes.Front);
        computeShader.SetTexture(iterativeTransformKernelId, "FrontBufferDownsampled", pingPongBufferDownsampled.Front);

        cubemapScreenBlitMaterial.SetTexture("_FrontBufferFullRes", pingPongBufferFullRes.Front);
        cubemapScreenBlitMaterial.SetTexture("_FrontBufferDownsampled", pingPongBufferDownsampled.Front);
        /*
        if (dispatchBackBuffer)
        {
            int dispatchWidth = pingPongBufferFullRes.Back.width;
            int dispatchHeight = pingPongBufferFullRes.Back.height;
            int threadGroupsX = Mathf.CeilToInt(dispatchWidth / (int)threadsX);
            int threadGroupsY = Mathf.CeilToInt(dispatchHeight / (int)threadsY);

            computeShader.SetVector("POSITION", previousPosition - transform.position);
            computeShader.SetBool("IS_DEPTH_NORMALIZED", false);
            computeShader.SetBool("IS_ABSOLUTE_POSITION", true);
            computeShader.SetBool("ONLY_DISPATCH_DOWNSAMPLED", dispatchDownsampled);
            computeShader.SetTexture(iterativeTransformKernelId, "Input", pingPongBufferFullRes.Back);
            computeShader.SetTexture(iterativeTransformKernelId, "InputDownsampled", pingPongBufferDownsampled.Back);

            computeShader.Dispatch(iterativeTransformKernelId, threadGroupsX, threadGroupsY, 6);
        }
        
        if (dispatchInput)
        {
            computeShader.SetVector("POSITION", -transform.position);
            computeShader.SetBool("IS_DEPTH_NORMALIZED", true);
            computeShader.SetBool("IS_ABSOLUTE_POSITION", false);

            computeShader.SetTexture(iterativeTransformKernelId, "Input", input);
            computeShader.SetTexture(iterativeTransformKernelId, "InputDownsampled", inputDownsampled);

            int dispatchWidth = input.width;
            int dispatchHeight = input.height;
            int threadGroupsX = Mathf.CeilToInt(resolution.x / (int)threadsX);
            int threadGroupsY = Mathf.CeilToInt(resolution.y / (int)threadsY);
            computeShader.Dispatch(iterativeTransformKernelId, threadGroupsX, threadGroupsY, input.volumeDepth);
        }
        */
        DispatchBackBufferComputeShader();
        DispatchInputComputeShader();

        previousPosition = transform.position;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, cubemapScreenBlitMaterial);
    }

    private void DispatchBackBufferComputeShader()
    {
        if (!dispatchBackBuffer) return;

        int dispatchWidth = pingPongBufferFullRes.Back.width;
        int dispatchHeight = pingPongBufferFullRes.Back.height;
        int threadGroupsX = Mathf.CeilToInt(dispatchWidth / (int)threadsX);
        int threadGroupsY = Mathf.CeilToInt(dispatchHeight / (int)threadsY);

        computeShader.SetBool("IS_DEPTH_NORMALIZED", false);
        computeShader.SetBool("IS_ABSOLUTE_POSITION", true);
        computeShader.SetVector("POSITION", previousPosition - transform.position);
        computeShader.SetVector("DISPATCH_RESOLUTION", new Vector2(dispatchWidth, dispatchHeight));
        computeShader.SetTexture(iterativeTransformKernelId, "Input", pingPongBufferFullRes.Back);
        computeShader.SetTexture(iterativeTransformKernelId, "InputDownsampled", pingPongBufferDownsampled.Back);

        computeShader.Dispatch(iterativeTransformKernelId, threadGroupsX, threadGroupsY, 6);
    }

    private void DispatchInputComputeShader()
    {
        if (!dispatchInput) return;

        // Set global shader parameters
        computeShader.SetBool("IS_DEPTH_NORMALIZED", true);
        computeShader.SetBool("IS_ABSOLUTE_POSITION", false);
        computeShader.SetVector("POSITION", -transform.position);
        computeShader.SetTexture(iterativeTransformKernelId, "Input", input);
        computeShader.SetTexture(iterativeTransformKernelId, "InputDownsampled", inputDownsampled);

        // Sort cubemap positions by distance (closest first), then reverse to get furthest first
        Vector4[] sortedPositions = map.cubemapPositions
            .Select((pos, index) => new Vector4(pos.x, pos.y, pos.z, index)) // Store original index in 'w'
            .OrderBy(v => Vector3.Distance((Vector3)v, transform.position))
            .Reverse() // Reverse to get furthest first
            .ToArray();

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
            computeShader.SetVectorArray("CUBE_POSITIONS", selectedPositions);
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
}
