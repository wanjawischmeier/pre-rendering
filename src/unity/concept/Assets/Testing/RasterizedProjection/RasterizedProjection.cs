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
    public bool wireframe;

    private Texture2D input;
    private RenderTexture result;
    private Material material;
    private Camera mainCamera;
    private int translationKernel;
    private uint threadGroupsX, threadGroupsY;

    private void Start()
    {
        byte[] rawInput = File.ReadAllBytes(rootDirectory + path);
        input = new Texture2D(0, 0, TextureFormat.RGBA64, false);
        input.LoadImage(rawInput);
        result = new RenderTexture(input.width, input.height, 1);
        result.enableRandomWrite = true;
        result.filterMode = FilterMode.Point; // JUST FOR TESTING!!!
        Vector2 resolution = new Vector2(input.width, input.height);

        mainCamera = GetComponent<Camera>();

        translationKernel = computeShader.FindKernel("Translate");
        
        computeShader.GetKernelThreadGroupSizes(translationKernel, out threadGroupsX, out threadGroupsY, out _);
        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", config.nclip);
        computeShader.SetFloat("FCLIP", config.fclip);
        computeShader.SetVector("RESOLUTION", resolution);
        computeShader.SetTexture(translationKernel, "Input", input);
        computeShader.SetTexture(translationKernel, "Result", result);

        material = new Material(shader);
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
        material.SetVector("RESOLUTION", resolution);
        material.SetTexture("_MainTex", input);
        material.SetTexture("_ProjTex", result);
    }

    private void Update()
    {
        Matrix4x4 t = Matrix4x4.Translate(transform.position);
        Matrix4x4 r = Matrix4x4.Rotate(transform.rotation);
        
        if (wireframe) computeShader.EnableKeyword("WIREFRAME");
        else computeShader.DisableKeyword("WIREFRAME");

        computeShader.SetFloat("CAM_NCLIP", mainCamera.nearClipPlane);
        computeShader.SetFloat("CAM_FCLIP", mainCamera.farClipPlane);
        computeShader.SetMatrix("TR", (t * r).inverse);
        computeShader.Dispatch(translationKernel, (int)(input.width / threadGroupsX), (int)(input.height / threadGroupsY), 1);

        material.SetInteger("DEBUG", wireframe ? 1 : 0);
        material.SetFloat("FOV", (180 - mainCamera.fieldOfView) * Mathf.Deg2Rad);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);

        if (mainCamera.clearFlags == CameraClearFlags.Nothing)
            return;

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = result;
        GL.Clear(false, true, Color.black);
        RenderTexture.active = rt;
    }
}
