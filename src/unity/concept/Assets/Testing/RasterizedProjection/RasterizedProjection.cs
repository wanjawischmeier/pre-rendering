using PreRendering;
using System.IO;
using UnityEngine;

public class RasterizedProjection : MonoBehaviour
{
    // RasterizedInverseProjection (RIP, haha)

    public string rootDirectory, path, file1, file2;

    public MapConfig config;
    public ComputeShader computeShader;
    public Shader shader;
    public Vector2Int geometryResolution;
    public Vector3 offset1, offset2;
    public bool wireframe;

    private Texture2D input1, input2;
    public RenderTexture depthBuffer1, depthBuffer2, interpolationBuffer1, interpolationBuffer2, result1, result2;
    private Material material;
    private Camera mainCamera;
    private Vector2Int resolution;
    private int translationKernel;
    private uint threadGroupsX, threadGroupsY;

    private void Start()
    {
        mainCamera = GetComponent<Camera>();
        translationKernel = computeShader.FindKernel("Transform");
        computeShader.GetKernelThreadGroupSizes(translationKernel, out threadGroupsX, out threadGroupsY, out _);

        string path1 = Path.Combine(rootDirectory, path, file1);
        string path2 = Path.Combine(rootDirectory, path, file2);
        byte[] rawInput1 = File.ReadAllBytes(path1);
        byte[] rawInput2 = File.ReadAllBytes(path2);
        input1 = new Texture2D(0, 0, TextureFormat.RGBA64, false);
        input2 = new Texture2D(0, 0, TextureFormat.RGBA64, false);
        input1.LoadImage(rawInput1);
        input2.LoadImage(rawInput2);

        resolution = new Vector2Int(Mathf.CeilToInt(geometryResolution.x / (float)threadGroupsX), Mathf.CeilToInt(geometryResolution.y / (float)threadGroupsY));
        depthBuffer1 = new RenderTexture(geometryResolution.x, geometryResolution.y, 1, RenderTextureFormat.RFloat);
        depthBuffer1.enableRandomWrite = true;
        depthBuffer2 = new RenderTexture(depthBuffer1);
        interpolationBuffer1 = new RenderTexture(depthBuffer1);
        interpolationBuffer2 = new RenderTexture(depthBuffer1);
        result1 = new RenderTexture(depthBuffer1);
        result1.format = RenderTextureFormat.ARGB64;
        result2 = new RenderTexture(result1);
        // result.filterMode = FilterMode.Point; // JUST FOR TESTING!!!

        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", config.nclip);
        computeShader.SetFloat("FCLIP", config.fclip);
        computeShader.SetVector("GEOMETRY_RESOLUTION", (Vector2)geometryResolution);

        material = new Material(shader);
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
        material.SetTexture("_MainTex1", input1);
        material.SetTexture("_MainTex2", input2);
        material.SetTexture("_DepthTex1", depthBuffer1);
        material.SetTexture("_DepthTex2", depthBuffer2);
        material.SetTexture("_InterpTex1", interpolationBuffer1);
        material.SetTexture("_InterpTex2", interpolationBuffer2);
        material.SetTexture("_ProjTex1", result1);
        material.SetTexture("_ProjTex2", result2);
    }

    private void Update()
    {
        Matrix4x4 translation = Matrix4x4.Translate(transform.position);
        Matrix4x4 rotation = Matrix4x4.Rotate(transform.rotation);
        Matrix4x4 transormationMatrix = (translation * rotation).inverse;

        if (wireframe) computeShader.EnableKeyword("WIREFRAME");
        else computeShader.DisableKeyword("WIREFRAME");

        computeShader.SetFloat("CAM_NCLIP", mainCamera.nearClipPlane);
        computeShader.SetFloat("CAM_FCLIP", mainCamera.farClipPlane);
        computeShader.SetMatrix("TR", transormationMatrix);

        computeShader.SetVector("OFFSET", offset1);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input1.width, input1.height));
        computeShader.SetTexture(translationKernel, "Input", input1);
        computeShader.SetTexture(translationKernel, "InterpolationBuffer", interpolationBuffer1);
        computeShader.SetTexture(translationKernel, "DepthBuffer", depthBuffer1);
        computeShader.SetTexture(translationKernel, "Result", result1);
        computeShader.Dispatch(translationKernel, resolution.x, resolution.y, 1);

        computeShader.SetVector("OFFSET", offset2);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input2.width, input2.height));
        computeShader.SetTexture(translationKernel, "Input", input2);
        computeShader.SetTexture(translationKernel, "DepthBuffer", depthBuffer2);
        computeShader.SetTexture(translationKernel, "InterpolationBuffer", interpolationBuffer2);
        computeShader.SetTexture(translationKernel, "Result", result2);
        computeShader.Dispatch(translationKernel, resolution.x, resolution.y, 1);

        material.SetInteger("DEBUG", wireframe ? 1 : 0);
        material.SetMatrix("TR", transormationMatrix);
        material.SetFloat("FOV", (180 - mainCamera.fieldOfView) * Mathf.Deg2Rad);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);

        if (mainCamera.clearFlags == CameraClearFlags.Nothing)
            return;
        
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = depthBuffer1;
        GL.Clear(false, true, new Color(mainCamera.farClipPlane, 0, 0, 0));
        RenderTexture.active = depthBuffer2;
        GL.Clear(false, true, new Color(mainCamera.farClipPlane, 0, 0, 0));
        RenderTexture.active = result1;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = result2;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = rt;
    }
}