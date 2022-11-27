using UnityEngine;

public class ImportanceBasedLoader : MonoBehaviour
{
    public Texture2D input;
    public ComputeShader computeShader;
    public float magnitude, offset;

    private RenderTexture result;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int kernel, threadGroupsX, threadGroupsY;

    private void Start()
    {
        kernel = computeShader.FindKernel("CSMain");
        computeShader.GetKernelThreadGroupSizes(kernel, out threadGroupSizeX, out threadGroupSizeY, out _);
        threadGroupsX = input.width / (int)threadGroupSizeX;
        threadGroupsY = input.height / (int)threadGroupSizeY;

        result = new RenderTexture(input.width, input.height, 0);
        result.enableRandomWrite = true;

        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetTexture(kernel, "Input", input);
        computeShader.SetTexture(kernel, "Result", result);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
    }

    private void Update()
    {
        computeShader.SetVector("OFFSET", transform.position);
        computeShader.SetFloat("MAG", magnitude);
        computeShader.SetFloat("OFF", offset);
        computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;
    }
}
