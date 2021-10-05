using UnityEngine;
using MapManagement;
using System.IO;
using System.Collections;

public class TextureLoader : MonoBehaviour
{
    public string renderPath;
    public string mapPath;
    public ComputeShader projectShader;
    public Shader postProcessing;
    public MovementController controller;
    [Range(1, 100)]
    public int maxTextures = 10;
    public Vector2Int geometryResolution;
    public bool debug;
    Vector3[] debugOffArray;
    public int selectedId = 1;

    FPSCounter debugDisplay;
    Map map;

    Material postProcessingMat;

    ComputeBuffer debugOffBuffer;
    ComputeBuffer offBuffer;
    public RenderTexture projected;

    int project, combine;
    uint projectThreadsX, projectThreadsY, combineThreadsX, combineThreadsY;
    int projectWidth, projectHeight;

    void Start()
    {
        string path = Path.Combine(renderPath, mapPath);
        map = new Map(path, maxTextures);

        projectWidth = geometryResolution.x;
        projectHeight = geometryResolution.y;

        AddDebugger("Debug");

        postProcessingMat = new Material(postProcessing);
        project = projectShader.FindKernel("Projection");
        combine = projectShader.FindKernel("Combine");

        projectShader.GetKernelThreadGroupSizes(project, out projectThreadsX, out projectThreadsY, out uint _);
        projectShader.GetKernelThreadGroupSizes(combine, out combineThreadsX, out combineThreadsY, out uint _);

        SetUpTextures();
        SetShaderConstants();
    }

    void Update()
    {
        HandleKeyPresses();
        SetShaderValues();

        map.LoadTexturesNearPosition(transform.position);

        debugOffArray[selectedId - 1] = controller.secondaryPosition;
        offBuffer.SetData(map.offArray);
        debugOffBuffer.SetData(debugOffArray);
        
        for (int i = 0; i < maxTextures; i++)
        {
            float distance = Vector3.Distance(transform.position, map.offArray[i]);
            // TODO: Resolution based on distance

            RenderTexture rt = RenderTexture.GetTemporary(projectWidth, projectHeight, 0, RenderTextureFormat.ARGB64);
            rt.enableRandomWrite = true;
            projectShader.SetTexture(combine, "_Input", rt);
            projectShader.SetTexture(project, "_Result", rt);
            projectShader.SetInt("IMG_IDX", i);

            projectShader.Dispatch(project, projectWidth / (int)projectThreadsX, projectHeight / (int)projectThreadsY, 1);
            projectShader.Dispatch(combine, projected.width / (int)combineThreadsX, projected.height / (int)combineThreadsY, 1);

            RenderTexture.ReleaseTemporary(rt);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination) =>
        Graphics.Blit(projected, destination, postProcessingMat);

    void OnDestroy()
    {
        if (offBuffer != null) offBuffer.Release();
        if (debugOffBuffer != null) debugOffBuffer.Release();
        if (projected != null) projected.Release();
    }

    void AddDebugger(string name)
    {
        debugDisplay = GameObject.Find(name).GetComponent<FPSCounter>();
        debugDisplay.selected = selectedId;
        debugDisplay.maxTextures = maxTextures;
        debugDisplay.textureResolution = new Vector2(
            map.config.textureWidth,
            map.config.textureHeight);
    }

    void SetUpTextures()
    {
        Resolution res = EstimatePanoramaResolution(Screen.width, Screen.height, Camera.main.fieldOfView);
        projected = new RenderTexture(res.width, res.height, 0, RenderTextureFormat.ARGB64);
        projected.enableRandomWrite = true;
        projected.Create();

        debugOffArray = new Vector3[maxTextures];
        offBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
        debugOffBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
    }

    void SetShaderConstants()
    {
        Shader.SetGlobalFloat("PI", Mathf.PI);
        Shader.SetGlobalFloat("PI2", Mathf.PI * 2);
        Shader.SetGlobalFloat("FCLIP", map.config.fclip);
        Shader.SetGlobalInt("MX_IDX", maxTextures);
        Shader.SetGlobalTexture("_InputArray", map.textures);
        Shader.SetGlobalTexture("_Projected", projected);

        projectShader.SetBuffer(project, "OffsetBuffer", offBuffer);
        projectShader.SetBuffer(project, "DebugOffsetBuffer", debugOffBuffer);
    }

    void SetShaderValues()
    {
        projectShader.SetVector("Position", transform.position);
        postProcessingMat.SetVector("Rotation", transform.eulerAngles * Mathf.Deg2Rad);
        postProcessingMat.SetFloat("FOV", (180 - Camera.main.fieldOfView) * Mathf.Deg2Rad);
        postProcessingMat.SetInt("Debug", debug ? 1 : 0);
    }

    void HandleKeyPresses()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        if (Input.GetKeyDown(KeyCode.F2)) debugDisplay.Toggle();
        if (Input.GetKeyDown(KeyCode.F3)) debug = !debug;
        if (Input.mouseScrollDelta.y > 0) selectedId += 1;
        if (Input.mouseScrollDelta.y < 0) selectedId -= 1;
        if (selectedId > maxTextures) selectedId = 1;
        if (selectedId < 1) selectedId = maxTextures;
        debugDisplay.selected = selectedId;
    }

    public Resolution EstimatePanoramaResolution(int width, int height, float fov)
    {
        Resolution res = new Resolution();
        res.width = Mathf.RoundToInt(width * (360 / fov));
        res.height = Mathf.RoundToInt(height * (180 / fov));
        return res;
    }
}