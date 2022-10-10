using UnityEngine;

public class ApplyWarp : MonoBehaviour
{
    public ComputeShader computeShader;
    public Shader warpShader;
    public Texture2D input;

    private RenderTexture result;
    private Material warpMaterial;
    private int kernelIndex, threadGroupsX, threadGroupsY;

    private void Start()
    {
        result = new RenderTexture(input.width, input.height, 0, RenderTextureFormat.ARGBFloat);
        result.enableRandomWrite = true;

        warpMaterial = new Material(warpShader);

        kernelIndex = computeShader.FindKernel("CSMain");
        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetTexture(kernelIndex, "Input", input);
        computeShader.SetTexture(kernelIndex, "Result", result);
        computeShader.GetKernelThreadGroupSizes(kernelIndex, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        threadGroupsX = input.width / (int)threadGroupSizeX;
        threadGroupsY = input.height / (int)threadGroupSizeY;
    }
    
    private void Update()
    {
        computeShader.SetVector("POSITION", transform.position);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;

        computeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);
        Graphics.Blit(result, destination, warpMaterial);
    }
}
