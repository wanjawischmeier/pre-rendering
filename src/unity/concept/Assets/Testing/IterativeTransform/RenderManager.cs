using UnityEngine;

public class RenderManager : MonoBehaviour  
{
    public Texture2D[] inputImages;
    public Vector4[] cubemapPositions;
    public Material cubemapScreenBlitMaterial;
    public ComputeShader computeShader;
    public Vector2Int downsampledResolution;
    public int cubemapSlice = 0;

    public RenderTexture input, inputOutput, downsampled, cubemap;
    int iterativeTransformKernelId, threadGroupsX, threadGroupsY;
    Vector3 previousPosition = Vector3.zero;

    private void Start()
    {
        iterativeTransformKernelId = computeShader.FindKernel("IterativeTransform");
        computeShader.GetKernelThreadGroupSizes(iterativeTransformKernelId, out uint threadsX, out uint threadsY, out uint _);

        int dispatchWidth = inputImages[0].width;
        int dispatchHeight = inputImages[0].height;
        threadGroupsX = Mathf.CeilToInt(dispatchWidth / (int)threadsX);
        threadGroupsY = Mathf.CeilToInt(dispatchHeight / (int)threadsY);

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

        computeShader.SetBool("IS_SETUP_FRAME", true);
        computeShader.SetVector("INOUT_RESOLUTION", new Vector2(dispatchWidth, dispatchHeight));
        computeShader.SetVector("DOWNSAMPLED_RESOLUTION", new Vector2(downsampledResolution.x, downsampledResolution.y));
        computeShader.SetVectorArray("CUBE_POSITIONS", cubemapPositions);
        computeShader.SetMatrixArray("ORIENTATION_MATRICIES", CubeMapConversion.orientationMatricies);
        computeShader.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);

        cubemapScreenBlitMaterial.SetTexture("_Cubemap", cubemap);
        cubemapScreenBlitMaterial.SetTexture("_InputOutput", inputOutput);
        cubemapScreenBlitMaterial.SetTexture("_Downsampled", downsampled);
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

        if (transform.position.Equals(previousPosition))
        {
            return;
        }

        computeShader.SetVector("POSITION", transform.position);
        computeShader.SetVectorArray("CUBE_POSITIONS", cubemapPositions);

        RenderTexture rt = RenderTexture.active;
        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            Graphics.SetRenderTarget(inputOutput, 0, CubemapFace.Unknown, faceIndex);
            // GL.Clear(true, true, Color.black);
        }
        RenderTexture.active = rt;

        computeShader.Dispatch(iterativeTransformKernelId, threadGroupsX, threadGroupsY, 6);

        if (transform.position != Vector3.zero && previousPosition == Vector3.zero)
        {
            computeShader.SetBool("IS_SETUP_FRAME", false);
        }
        previousPosition = transform.position;
        computeShader.SetVector("PREVIOUS_POSITION", previousPosition);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, cubemapScreenBlitMaterial);
    }
}
