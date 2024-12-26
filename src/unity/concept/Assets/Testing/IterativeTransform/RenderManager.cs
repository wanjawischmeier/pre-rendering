using UnityEngine;

public class RenderManager : MonoBehaviour  
{
    public Texture2D[] inputImages;
    public Vector4[] cubemapPositions;
    public Material cubemapScreenBlitMaterial;
    public ComputeShader computeShader;
    public Vector2Int downsampledResolution;
    public int cubemapSlice = 0;
    [Range(2, maxPositionBufferSize)]
    public int positionBufferSize = 10;

    public RenderTexture input, inputOutput, downsampled, cubemap;
    int positionBufferIndex = -1;
    int iterativeTransformKernelId, dispatchWidth, dispatchHeight;
    uint threadGroupsX, threadGroupsY;
    Vector4[] positionBuffer;

    const int maxPositionBufferSize = 100;

    private void Start()
    {
        iterativeTransformKernelId = computeShader.FindKernel("IterativeTransform");
        computeShader.GetKernelThreadGroupSizes(iterativeTransformKernelId, out threadGroupsX, out threadGroupsY, out uint _);
        
        dispatchWidth = inputImages[0].width;
        dispatchHeight = inputImages[0].height;

        input = new RenderTexture(dispatchWidth, dispatchHeight, 0);
        input.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        input.format = RenderTextureFormat.ARGBHalf;
        input.enableRandomWrite = true;
        input.volumeDepth = 6;

        inputOutput = new RenderTexture(dispatchWidth, dispatchHeight, 0);
        inputOutput.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        inputOutput.format = RenderTextureFormat.ARGBHalf;
        inputOutput.enableRandomWrite = true;
        inputOutput.volumeDepth = 6;

        downsampled = new RenderTexture(downsampledResolution.x, downsampledResolution.y, 0);
        downsampled.dimension = UnityEngine.Rendering.TextureDimension.Tex2DArray;
        inputOutput.format = RenderTextureFormat.ARGBHalf;
        downsampled.enableRandomWrite = true;
        downsampled.volumeDepth = 6;

        for (int i = 0; i < Mathf.Min(inputOutput.volumeDepth, inputImages.Length); i++)
        {
            Graphics.Blit(inputImages[i], input, 0, i);
            Graphics.Blit(inputImages[i], inputOutput, 0, i);
        }
        
        positionBuffer = new Vector4[positionBufferSize];

        computeShader.SetInt("POSITION_BUFFER_SIZE", positionBufferSize);
        computeShader.SetVector("INOUT_RESOLUTION", new Vector2(dispatchWidth, dispatchHeight));
        computeShader.SetVector("DOWNSAMPLED_RESOLUTION", new Vector2(downsampledResolution.x, downsampledResolution.y));
        computeShader.SetVectorArray("CUBE_POSITIONS", cubemapPositions);
        computeShader.SetMatrixArray("ORIENTATION_MATRICIES", CubeMapConversion.orientationMatricies);
        computeShader.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);

        cubemapScreenBlitMaterial.SetTexture("_Cubemap", cubemap);
        cubemapScreenBlitMaterial.SetTexture("_InputOutput", inputOutput);
        cubemapScreenBlitMaterial.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);
        cubemapScreenBlitMaterial.SetVectorArray("CUBE_POSITIONS", cubemapPositions);

        computeShader.SetTexture(iterativeTransformKernelId, "Input", input);
        computeShader.SetTexture(iterativeTransformKernelId, "InputOutput", inputOutput);
        computeShader.SetTexture(iterativeTransformKernelId, "Downsampled", downsampled);
    }

    private void Update()
    {
        Matrix4x4 viewToWorldMatrix = Camera.main.cameraToWorldMatrix;
        Matrix4x4 invProjMatrix = Camera.main.projectionMatrix.inverse;

        cubemapScreenBlitMaterial.SetMatrix("_InvProjectionMatrix", invProjMatrix);
        cubemapScreenBlitMaterial.SetMatrix("_ViewToWorldMatrix", viewToWorldMatrix);

        bool isInitialSetupFrame = positionBufferIndex == -1;
        if (!isInitialSetupFrame && transform.position.Equals(positionBuffer[positionBufferIndex]))
        {
            return;
        }

        computeShader.SetVector("POSITION", transform.position);
        computeShader.SetVectorArray("CUBE_POSITIONS", cubemapPositions);

        if (isInitialSetupFrame)
        {
            // the buffer will detect an overflow by default
            computeShader.SetBool("IGNORE_BUFFER_OVERFLOW", true);
            positionBufferIndex++;
        }
        computeShader.SetInt("POSITION_BUFFER_INDEX", positionBufferIndex);

        RenderTexture rt = RenderTexture.active;
        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            Graphics.SetRenderTarget(inputOutput, 0, CubemapFace.Unknown, faceIndex);
            // GL.Clear(true, true, Color.black);
        }
        RenderTexture.active = rt;

        computeShader.Dispatch(iterativeTransformKernelId, dispatchWidth / (int)threadGroupsX, dispatchHeight / (int)threadGroupsY, 6);

        if (isInitialSetupFrame)
        {
            // reenable buffer overflow detection
            computeShader.SetBool("IGNORE_BUFFER_OVERFLOW", false);
        }

        // push current position to circular buffer
        positionBuffer[positionBufferIndex] = transform.position;
        positionBufferIndex = (positionBufferIndex + 1) % positionBufferSize;
        computeShader.SetVectorArray("POSITION_BUFFER", positionBuffer);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, cubemapScreenBlitMaterial);
    }
}
