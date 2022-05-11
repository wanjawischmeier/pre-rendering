using UnityEngine;

public class Importance : MonoBehaviour
{
    public Texture2D texture;
    public ComputeShader computeShader;
    public float nclip, fclip;
    public DebugMode debug;
    DebugMode lastDebug = DebugMode.TexCoords;
    Vector3 lastPosition = Vector3.zero;
    RenderTexture result;
    int kernel;

    public enum DebugMode
    {
        Disabled,
        TexCoords,
        Projected,
        Heatmap,
        Difference,
        Optimal
    }

    private void Start()
    {
        result = new RenderTexture(texture.width, texture.height, 0);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point;

        kernel = computeShader.FindKernel("Importance");
        computeShader.SetTexture(kernel, "Input", texture);
        computeShader.SetTexture(kernel, "Result", result);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", nclip);
        computeShader.SetFloat("FCLIP", fclip);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(texture.width, texture.height));
    }

    private void Update()
    {
        if (debug != lastDebug)
        {
            lastDebug = debug;
            computeShader.SetInt("DEBUG", (int)debug);
            Project();
        }

        if (transform.position != lastPosition)
        {
            lastPosition = transform.position;
            computeShader.SetVector("POSITION", transform.position);
            Project();
        }
    }

    private void Project()
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;

        computeShader.Dispatch(kernel, texture.width, texture.height, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
