using UnityEngine;
using MapManagement;
using System;
using System.IO;

public class TextureLoader : MonoBehaviour
{
    public string renderPath;
    public string mapPath;
    public ComputeShader projectShader;
    // public Shader postProcessing;
    public MovementController controller;
    [Range(1, 100)]
    public int maxTextures = 10;
    public Vector2Int geometryResolution;
    public bool debug;
    public bool fill;
    public float fillOff;
    Vector3[] debugOffArray;
    public int selectedId = 1;

    FPSCounter debugDisplay;
    Map map;

    // Material postProcessingMat;
    Texture2DArray textureArray;
    Vector3[] offArray;
    ComputeBuffer debugOffBuffer;
    ComputeBuffer offBuffer;
    public RenderTexture projected;
    public RenderTexture result;

    int project, gnomonic;
    uint projectThreadsX, projectThreadsY;
    uint gnomonicThreadsX, gnomonicThreadsY;
    int screenWidth, screenHeight;
    int projectWidth, projectHeight;

    void Start()
    {
        string path = Path.Combine(renderPath, mapPath);
        map = new Map(path);

        screenWidth = Screen.width;
        screenHeight = Screen.height;
        projectWidth = geometryResolution.x;
        projectHeight = geometryResolution.y;

        AddDebugger("Debug");

        // postProcessingMat = new Material(postProcessing);
        project = projectShader.FindKernel("Projection");
        gnomonic = projectShader.FindKernel("Gnomonic");
        projectShader.GetKernelThreadGroupSizes(project, out projectThreadsX, out projectThreadsY, out uint _);
        projectShader.GetKernelThreadGroupSizes(gnomonic, out gnomonicThreadsX, out gnomonicThreadsY, out uint _);

        SetUpTextures();
        SetComputeShaderConstants();
    }

    void Update()
    {
        HandleKeyPresses();
        SetComputeShaderValues();

        offArray = map.GetClosest(transform.position, maxTextures);
        debugOffArray[selectedId - 1] = controller.secondaryPosition;
        offBuffer.SetData(offArray);
        debugOffBuffer.SetData(debugOffArray);

        map.SetTexturesAtPositions(offArray, ref textureArray);
        
        projectShader.Dispatch(project, projectWidth / (int)projectThreadsX, projectHeight / (int)projectThreadsY, maxTextures);
        projectShader.Dispatch(gnomonic, screenWidth / (int)gnomonicThreadsX, screenHeight / (int)gnomonicThreadsY, 1);
    }

    void OnDestroy()
    {
        if (offBuffer != null) offBuffer.Release();
        if (debugOffBuffer != null) debugOffBuffer.Release();
        if (result != null) result.Release();
    }


    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // Graphics.Blit(result, destination, postProcessingMat);
        Graphics.Blit(result, destination);
        
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = projected;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;
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
        textureArray = new Texture2DArray(map.config.textureWidth, map.config.textureHeight, maxTextures, TextureFormat.RGBA32, 1, false);
        projected = new RenderTexture(projectWidth, projectHeight, 24, RenderTextureFormat.ARGBFloat);
        result = new RenderTexture(screenWidth, screenHeight, 24);
        projected.enableRandomWrite = true;
        result.enableRandomWrite = true;
        projected.Create();
        result.Create();
        offArray = new Vector3[maxTextures];
        debugOffArray = new Vector3[maxTextures];
        offBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
        debugOffBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
    }

    void SetComputeShaderConstants()
    {
        projectShader.SetFloat("PI", Mathf.PI);
        projectShader.SetFloat("PI2", Mathf.PI * 2);
        projectShader.SetFloat("FCLIP", map.config.fclip);
        projectShader.SetBuffer(project, "OffsetBuffer", offBuffer);
        projectShader.SetBuffer(project, "DebugOffsetBuffer", debugOffBuffer);
        projectShader.SetTexture(project, "InputArray", textureArray);
        projectShader.SetTexture(project, "Projected", projected);

        // postProcessingMat.SetFloat("PI", Mathf.PI);
        // postProcessingMat.SetFloat("PI2", Mathf.PI * 2);
        // postProcessingMat.SetFloat("FCLIP", map.config.fclip);
        // postProcessingMat.SetTexture("InputArray", textureArray);
        // postProcessingMat.SetTexture("ProjectedIn", projected);
        // postProcessingMat.SetTexture("Result", result);
        projectShader.SetTexture(gnomonic, "InputArray", textureArray);
        projectShader.SetTexture(gnomonic, "ProjectedIn", projected);
        projectShader.SetTexture(gnomonic, "Result", result);
    }

    void SetComputeShaderValues()
    {
        projectShader.SetVector("Position", transform.position);
        projectShader.SetVector("Rotation", transform.eulerAngles * Mathf.Deg2Rad);
        projectShader.SetFloat("FOV", (180 - Camera.main.fieldOfView) * Mathf.Deg2Rad);
        projectShader.SetFloat("Off", fillOff);
        projectShader.SetBool("Debug", debug);
    }

    void HandleKeyPresses()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        if (Input.GetKeyDown(KeyCode.F3)) debug = !debug;
        if (Input.GetKeyDown(KeyCode.F4)) fill = !fill;
        if (Input.mouseScrollDelta.y > 0) selectedId += 1;
        if (Input.mouseScrollDelta.y < 0) selectedId -= 1;
        if (selectedId > maxTextures) selectedId = 1;
        if (selectedId < 1) selectedId = maxTextures;
        debugDisplay.selected = selectedId;
    }
}