using UnityEngine;

public class NormalPlaneImplementation : MonoBehaviour
{
    public Texture2D input;
    public ComputeShader computeShader;
    public int range = 10;

    private RenderTexture result;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int kernel, threadGroupsX, threadGroupsY;

    private void Start()
    {
        result = new RenderTexture(input.width, input.height, 0);
        result.enableRandomWrite = true;

        kernel = computeShader.FindKernel("Transform");
        computeShader.GetKernelThreadGroupSizes(kernel, out threadGroupSizeX, out threadGroupSizeY, out _);
        threadGroupsX = input.width / (int)threadGroupSizeX;
        threadGroupsY = input.height / (int)threadGroupSizeY;

        computeShader.SetTexture(kernel, "Input", input);
        computeShader.SetTexture(kernel, "Result", result);
        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
    }

    private void Update()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;

        computeShader.SetVector("OFFSET", transform.position);
        computeShader.SetInt("RANGE", range);
        computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
