using UnityEngine;

public class ApplyWarp : MonoBehaviour
{
    public ComputeShader computeShader;
    public Shader warpShader;
    public Texture2D input0, input1;
    public Vector2 uv;
    public Vector2Int PL, PR, PT, PB;
    public Vector3 offset0, offset1;

    private RenderTexture projected, result;
    private Material warpMaterial;
    private int projectKernel, interpolateKernel, threadGroupsX, threadGroupsY;

    private void Start()
    {
        projected = new RenderTexture(input0.width, input0.height, 0, RenderTextureFormat.ARGBFloat);
        projected.enableRandomWrite = true;
        result = new RenderTexture(projected);
        warpMaterial = new Material(warpShader);

        projectKernel = computeShader.FindKernel("Project");
        interpolateKernel = computeShader.FindKernel("Interpolate");
        
        computeShader.SetVector("RESOLUTION", new Vector2(input0.width, input0.height));
        computeShader.SetTexture(projectKernel, "Projected", projected);
        computeShader.SetTexture(interpolateKernel, "ProjectedInput", projected);
        computeShader.SetTexture(interpolateKernel, "Result", result);
        computeShader.GetKernelThreadGroupSizes(projectKernel, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        threadGroupsX = input0.width / (int)threadGroupSizeX;
        threadGroupsY = input0.height / (int)threadGroupSizeY;
    }
    
    private void Update()
    {
        computeShader.SetVector("POSITION", transform.position);
        computeShader.SetVector("UV", uv);
        computeShader.SetVector("PL", new Vector2(PL.x, PL.y));
        computeShader.SetVector("PR", new Vector2(PR.x, PR.y));
        computeShader.SetVector("PT", new Vector2(PT.x, PT.y));
        computeShader.SetVector("PB", new Vector2(PB.x, PB.y));
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = projected;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;

        computeShader.SetVector("OFFSET", offset0);
        computeShader.SetTexture(projectKernel, "Input", input0);
        computeShader.Dispatch(projectKernel, threadGroupsX, threadGroupsY, 1);
        computeShader.SetVector("OFFSET", offset1);
        computeShader.SetTexture(projectKernel, "Input", input1);
        computeShader.Dispatch(projectKernel, threadGroupsX, threadGroupsY, 1);
        computeShader.Dispatch(interpolateKernel, threadGroupsX, threadGroupsY, 1);
        Graphics.Blit(result, destination, warpMaterial);
    }
}
