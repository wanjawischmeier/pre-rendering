using PreRendering;
using System.IO;
using UnityEngine;

public class RasterizedProjection : MonoBehaviour
{
    // RasterizedInverseProjection (RIP, haha)

    public string rootDirectory;
    public string path;
    public MapConfig config;
    public ComputeShader computeShader;
    public Shader shader;
    public Vector2Int geometryResolution;
    public bool wireframe;

    private Texture2D input;
    private RenderTexture depthBuffer, result;
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

        byte[] rawInput = File.ReadAllBytes(rootDirectory + path);
        input = new Texture2D(0, 0, TextureFormat.RGBA64, false);
        input.LoadImage(rawInput);
        resolution = new Vector2Int(Mathf.CeilToInt(geometryResolution.x / (float)threadGroupsX), Mathf.CeilToInt(geometryResolution.y / (float)threadGroupsY));
        depthBuffer = new RenderTexture(geometryResolution.x, geometryResolution.y, 1, RenderTextureFormat.RFloat);
        depthBuffer.enableRandomWrite = true;
        result = new RenderTexture(depthBuffer);
        result.format = RenderTextureFormat.RG32;
        // result.filterMode = FilterMode.Point; // JUST FOR TESTING!!!

        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", config.nclip);
        computeShader.SetFloat("FCLIP", config.fclip);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(input.width, input.height));
        computeShader.SetVector("GEOMETRY_RESOLUTION", (Vector2)geometryResolution);
        computeShader.SetTexture(translationKernel, "Input", input);
        computeShader.SetTexture(translationKernel, "DepthBuffer", depthBuffer);
        computeShader.SetTexture(translationKernel, "Result", result);

        material = new Material(shader);
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
        material.SetTexture("_MainTex", input);
        material.SetTexture("_ProjTex", result);
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
        RenderTexture.active = depthBuffer;
        GL.Clear(false, true, new Color(mainCamera.farClipPlane, 0, 0, 0));
        RenderTexture.active = result;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = rt;
    }
}
