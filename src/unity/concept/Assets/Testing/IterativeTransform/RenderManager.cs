using UnityEngine;

public class RenderManager : MonoBehaviour
{
    public Texture2D[] inputImages;
    public Vector4[] cubemapPositions;
    public ComputeShader computeShader;
    public Vector2Int downsampledResolution;

    public RenderTexture input, inputOutput, downsampled;
    int iterativeTransformKernelId, dispatchWidth, dispatchHeight;
    uint threadGroupsX, threadGroupsY;

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

        computeShader.SetVector("INOUT_RESOLUTION", new Vector2(dispatchWidth, dispatchHeight));
        computeShader.SetVector("DOWNSAMPLED_RESOLUTION", new Vector2(downsampledResolution.x, downsampledResolution.y));
        computeShader.SetVectorArray("CUBE_POSITIONS", cubemapPositions);
        computeShader.SetMatrixArray("ORIENTATION_MATRICIES", CubeMapConversion.orientationMatricies);
        computeShader.SetMatrixArray("INVERSE_ORIENTATION_MATRICIES", CubeMapConversion.inverseOrientationMatricies);

        computeShader.SetTexture(iterativeTransformKernelId, "Input", input);
        computeShader.SetTexture(iterativeTransformKernelId, "InputOutput", inputOutput);
        computeShader.SetTexture(iterativeTransformKernelId, "Downsampled", downsampled);
    }

    private void Update()
    {
        computeShader.SetVector("POSITION", transform.position);
        computeShader.Dispatch(iterativeTransformKernelId, dispatchWidth / (int)threadGroupsX, dispatchHeight / (int)threadGroupsY, 6);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // TODO: custom frag shader
        Graphics.Blit(source, destination);
    }
}
