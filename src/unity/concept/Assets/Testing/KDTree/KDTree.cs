using UnityEngine;

public class KDTree: MonoBehaviour
{
    public Texture2D input;
    public ComputeShader computeShader;

    private RenderTexture result, tree;
    private Vector3 lastPosition = Vector3.zero;
    private uint threadGroupSizeX, threadGroupSizeY;
    private int kernel, renderKernel, threadGroupsX, threadGroupsY;

    private void Start()
    {
        result = new RenderTexture(input.width, input.height, 0);
        result.enableRandomWrite = true;
        tree = new RenderTexture(result);
        tree.format = RenderTextureFormat.ARGBInt;

        kernel = computeShader.FindKernel("Transform");
        renderKernel = computeShader.FindKernel("RenderTree");
        computeShader.GetKernelThreadGroupSizes(kernel, out threadGroupSizeX, out threadGroupSizeY, out _);
        threadGroupsX = input.width / (int)threadGroupSizeX;
        threadGroupsY = input.height / (int)threadGroupSizeY;

        computeShader.SetTexture(kernel, "Input", input);
        // computeShader.SetTexture(kernel, "Result", result);
        computeShader.SetTexture(kernel, "Tree", tree);
        computeShader.SetTexture(renderKernel, "Input", input);
        computeShader.SetTexture(renderKernel, "Result", result);
        computeShader.SetTexture(renderKernel, "Tree", tree);
        computeShader.SetVector("RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
    }

    private void Update()
    {
        if (transform.position == lastPosition)
            return;
        else
            lastPosition = transform.position;

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = tree;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;
        
        computeShader.SetVector("OFFSET", transform.position);
        computeShader.Dispatch(kernel, threadGroupsX, threadGroupsY, 1);
        computeShader.Dispatch(renderKernel, threadGroupsX, threadGroupsY, 1);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}