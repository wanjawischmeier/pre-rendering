using UnityEngine;

public class RenderManager : MonoBehaviour  
{
    public Texture2D[] inputImages;
    public Vector4[] cubemapPositions;
    public Material cubemapScreenBlitMaterial;
    public ComputeShader computeShader;
    public Vector2Int downsampledResolution;
    public int cubemapSlice = 0;
    public bool dispatchInput, dispatchBackBuffer;
    public RenderTexture[] fbBufferFullRes, fbBufferDownsampled;
    public RenderTexture input, downsampled, cubemap;

    int iterativeTransformKernelId, threadGroupsX, threadGroupsY;
    Vector3 previousPosition = Vector3.one;
    PingPongBuffer pingPongBufferFullRes, pingPongBufferDownsampled;

    private void Start()
    {
        iterativeTransformKernelId = computeShader.FindKernel("IterativeTransform");
        computeShader.GetKernelThreadGroupSizes(iterativeTransformKernelId, out uint threadsX, out uint threadsY, out uint _);

        int dispatchWidth = inputImages[0].width;
        int dispatchHeight = inputImages[0].height;
        threadGroupsX = Mathf.CeilToInt(dispatchWidth / (int)threadsX);
        threadGroupsY = Mathf.CeilToInt(dispatchHeight / (int)threadsY);

        pingPongBufferFullRes = new PingPongBuffer(dispatchWidth, dispatchHeight, RenderTextureFormat.ARGBHalf);
        fbBufferFullRes = pingPongBufferFullRes.Textures;

        pingPongBufferDownsampled = new PingPongBuffer(downsampledResolution.x, downsampledResolution.y, RenderTextureFormat.ARGBHalf);
        fbBufferDownsampled = pingPongBufferDownsampled.Textures;

        input = new RenderTexture(dispatchWidth, dispatchHeight, 0);
        input.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        input.format = RenderTextureFormat.ARGBHalf;
        input.enableRandomWrite = true;
        input.volumeDepth = inputImages.Length; // expected to be multiple of 6

        /*
        inputOutput = new RenderTexture(dispatchWidth, dispatchHeight, 0);
        inputOutput.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        inputOutput.format = RenderTextureFormat.ARGBHalf;
        inputOutput.enableRandomWrite = true;
        inputOutput.volumeDepth = 6
        
        downsampled = new RenderTexture(downsampledResolution.x, downsampledResolution.y, 0);
        downsampled.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        downsampled.format = RenderTextureFormat.ARGBHalf;
        downsampled.enableRandomWrite = true;
        downsampled.volumeDepth = 6;
        */

        for (int cubemapIndex = 0; cubemapIndex < input.volumeDepth; cubemapIndex++)
        {
            Graphics.Blit(inputImages[cubemapIndex], input, 0, cubemapIndex);
            // Graphics.Blit(inputImages[i], inputOutput, 0, i);
        }
        
        computeShader.SetVector("INOUT_RESOLUTION", new Vector2(dispatchWidth, dispatchHeight));
        computeShader.SetVector("DOWNSAMPLED_RESOLUTION", new Vector2(downsampledResolution.x, downsampledResolution.y));
        computeShader.SetVectorArray("CUBE_POSITIONS", cubemapPositions);
        computeShader.SetMatrixArray("ORIENTATION_MATRICIES", CubeMapConversion.orientationMatricies);
        computeShader.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);
        // computeShader.SetTexture(iterativeTransformKernelId, "InputOutput", inputOutput);
        // computeShader.SetTexture(iterativeTransformKernelId, "Downsampled", downsampled);

        cubemapScreenBlitMaterial.SetTexture("_Cubemap", cubemap);
        cubemapScreenBlitMaterial.SetTexture("_Input", input);
        // cubemapScreenBlitMaterial.SetTexture("_InputOutput", inputOutput);
        // cubemapScreenBlitMaterial.SetTexture("_Downsampled", downsampled);
        cubemapScreenBlitMaterial.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);
        cubemapScreenBlitMaterial.SetVectorArray("CUBE_POSITIONS", cubemapPositions);
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

        // computeShader.SetVector("POSITION", transform.position);
        computeShader.SetVectorArray("CUBE_POSITIONS", cubemapPositions);
        computeShader.SetTexture(iterativeTransformKernelId, "FrontBufferFullRes", pingPongBufferFullRes.Front);
        computeShader.SetTexture(iterativeTransformKernelId, "FrontBufferDownsampled", pingPongBufferDownsampled.Front);

        if (dispatchBackBuffer)
        {
            computeShader.SetVector("POSITION", previousPosition - transform.position);
            computeShader.SetBool("IS_DEPTH_NORMALIZED", false);
            computeShader.SetBool("IS_ABSOLUTE_POSITION", true);
            computeShader.SetBool("IS_DOWNSAMPLED_INPUT_AVAILABLE", true);
            computeShader.SetTexture(iterativeTransformKernelId, "Input", pingPongBufferFullRes.Back);
            computeShader.SetTexture(iterativeTransformKernelId, "InputDownsampled", pingPongBufferDownsampled.Back);

            computeShader.Dispatch(iterativeTransformKernelId, threadGroupsX, threadGroupsY, 6);
        }

        if (dispatchInput)
        {
            cubemapScreenBlitMaterial.SetVectorArray("CUBE_POSITIONS", cubemapPositions);
            Vector3 cubemapPosition = cubemapPositions[0];
            computeShader.SetVector("POSITION", cubemapPosition - transform.position);
            computeShader.SetBool("IS_DEPTH_NORMALIZED", true);
            computeShader.SetBool("IS_ABSOLUTE_POSITION", false);
            computeShader.SetBool("IS_DOWNSAMPLED_INPUT_AVAILABLE", false);
            computeShader.SetTexture(iterativeTransformKernelId, "Input", input);

            computeShader.Dispatch(iterativeTransformKernelId, threadGroupsX, threadGroupsY, input.volumeDepth);
        }

        cubemapScreenBlitMaterial.SetTexture("_Front", pingPongBufferFullRes.Front);
        cubemapScreenBlitMaterial.SetTexture("_Back", pingPongBufferFullRes.Front);

        previousPosition = transform.position;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, cubemapScreenBlitMaterial);
    }
}
