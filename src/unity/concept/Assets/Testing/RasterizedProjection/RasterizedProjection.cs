using PreRendering;
using System.IO;
using UnityEngine;

public class RasterizedProjection : MonoBehaviour
{
    // RasterizedInverseProjection (RIP, haha)

    public string rootDirectory, path, file0; //, file1;

    public MapConfig config;
    public ComputeShader computeShader;
    public Shader shader;
    public Vector2Int geometryResolution;
    public Color backgroundColor;
    public Vector3 offset0, offset1;
    public bool wireframe;

    private Texture2D input0; // , input1;
    public RenderTexture depthBuffer0, depthBuffer1, interpolationBuffer0, interpolationBuffer1, result0, result1;
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

        string path0 = Path.Combine(rootDirectory, path, file0);
        // string path1 = Path.Combine(rootDirectory, path, file1);
        byte[] rawInput0 = File.ReadAllBytes(path0);
        // byte[] rawInput1 = File.ReadAllBytes(path1);
        input0 = new Texture2D(0, 0, TextureFormat.RGBA64, false);
        // input1 = new Texture2D(0, 0, TextureFormat.RGBA64, false);
        input0.LoadImage(rawInput0);
        // input1.LoadImage(rawInput1);

        resolution = new Vector2Int(Mathf.CeilToInt(geometryResolution.x / (float)threadGroupsX), Mathf.CeilToInt(geometryResolution.y / (float)threadGroupsY));
        depthBuffer0 = new RenderTexture(geometryResolution.x, geometryResolution.y, 1, RenderTextureFormat.RFloat);
        depthBuffer0.enableRandomWrite = true;
        depthBuffer1 = new RenderTexture(depthBuffer0);
        interpolationBuffer0 = new RenderTexture(depthBuffer0);
        interpolationBuffer1 = new RenderTexture(depthBuffer0);
        result0 = new RenderTexture(depthBuffer0);
        result0.format = RenderTextureFormat.ARGB64;
        result1 = new RenderTexture(result0);
        // result.filterMode = FilterMode.Point; // JUST FOR TESTING!!!

        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", config.nclip);
        computeShader.SetFloat("FCLIP", config.fclip);
        computeShader.SetVector("GEOMETRY_RESOLUTION", (Vector2)geometryResolution);
        // computeShader.DisableKeyword("WIREFRAME");

        material = new Material(shader);
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
        material.SetTexture("_MainTex0", input0);
        // material.SetTexture("_MainTex1", input1);
        material.SetTexture("_DepthTex0", depthBuffer0);
        material.SetTexture("_DepthTex1", depthBuffer1);
        material.SetTexture("_InterpTex0", interpolationBuffer0);
        material.SetTexture("_InterpTex1", interpolationBuffer1);
        material.SetTexture("_ProjTex0", result0);
        material.SetTexture("_ProjTex1", result1);
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

        computeShader.SetVector("OFFSET", offset0);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input0.width, input0.height));
        computeShader.SetTexture(translationKernel, "Input", input0);
        computeShader.SetTexture(translationKernel, "InterpolationBuffer", interpolationBuffer0);
        computeShader.SetTexture(translationKernel, "DepthBuffer", depthBuffer0);
        computeShader.SetTexture(translationKernel, "Result", result0);
        computeShader.Dispatch(translationKernel, resolution.x, resolution.y, 1);
        /*
        computeShader.SetVector("OFFSET", offset1);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input1.width, input1.height));
        computeShader.SetTexture(translationKernel, "Input", input1);
        computeShader.SetTexture(translationKernel, "DepthBuffer", depthBuffer1);
        computeShader.SetTexture(translationKernel, "InterpolationBuffer", interpolationBuffer1);
        computeShader.SetTexture(translationKernel, "Result", result1);
        computeShader.Dispatch(translationKernel, resolution.x, resolution.y, 1);
        */
        material.SetInteger("DEBUG", wireframe ? 1 : 0);
        material.SetFloat("FOV", (180 - mainCamera.fieldOfView) * Mathf.Deg2Rad);
        material.SetFloat("CAM_FCLIP", mainCamera.farClipPlane);
        material.SetColor("BACK_COL", backgroundColor);
        material.SetMatrix("TR", transormationMatrix);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);

        if (mainCamera.clearFlags == CameraClearFlags.Nothing)
            return;
        
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = depthBuffer0;
        GL.Clear(false, true, new Color(mainCamera.farClipPlane, 0, 0, 0));
        RenderTexture.active = depthBuffer1;
        GL.Clear(false, true, new Color(mainCamera.farClipPlane, 0, 0, 0));
        RenderTexture.active = result0;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = result1;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = rt;
    }
}