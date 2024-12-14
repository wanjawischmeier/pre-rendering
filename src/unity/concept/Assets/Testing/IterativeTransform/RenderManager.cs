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
    int iterativeTransformKernelId, dispatchWidth, dispatchHeight;
    uint threadGroupsX, threadGroupsY;
    Vector3 previousPosition = Vector3.zero;

    private RenderTexture CopyTexturesToCubemap(Texture2D[] textures)
    {
        if (textures.Length != 6)
        {
            Debug.LogError("You must provide exactly 6 Texture2D objects for a cubemap.");
            return null;
        }

        var cubemap = new RenderTexture(dispatchWidth, dispatchHeight, 0);
        cubemap.dimension = UnityEngine.Rendering.TextureDimension.Cube;
        cubemap.format = RenderTextureFormat.ARGBHalf;
        cubemap.enableRandomWrite = true;
        cubemap.volumeDepth = 6;

        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            Graphics.CopyTexture(textures[faceIndex], 0, cubemap, faceIndex);
        }

        return cubemap;
    }

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

        cubemap = CopyTexturesToCubemap(inputImages);

        for (int i = 0; i < Mathf.Min(inputOutput.volumeDepth, inputImages.Length); i++)
        {
            Graphics.Blit(inputImages[i], input, 0, i);
            Graphics.Blit(inputImages[i], inputOutput, 0, i);
        }

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
        computeShader.SetVector("POSITION", transform.position);
        computeShader.SetVector("PREVIOUS_POSITION", previousPosition);
        computeShader.SetVectorArray("CUBE_POSITIONS", cubemapPositions);

        RenderTexture rt = RenderTexture.active;
        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            Graphics.SetRenderTarget(inputOutput, 0, CubemapFace.Unknown, faceIndex);
            // GL.Clear(true, true, Color.black);
        }
        RenderTexture.active = rt;

        Matrix4x4 viewToWorldMatrix = Camera.main.cameraToWorldMatrix;
        Matrix4x4 invProjMatrix = Camera.main.projectionMatrix.inverse;

        cubemapScreenBlitMaterial.SetMatrix("_InvProjectionMatrix", invProjMatrix);
        cubemapScreenBlitMaterial.SetMatrix("_ViewToWorldMatrix", viewToWorldMatrix);

        computeShader.Dispatch(iterativeTransformKernelId, dispatchWidth / (int)threadGroupsX, dispatchHeight / (int)threadGroupsY, 6);

        previousPosition = transform.position;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // TODO: custom frag shader
        // Graphics.Blit(inputOutput, destination, cubemapSlice, 0);
        Graphics.Blit(source, destination, cubemapScreenBlitMaterial);
    }
}
