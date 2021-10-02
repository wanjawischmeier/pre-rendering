using UnityEngine;
using MapManagement;
using System.IO;

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
    Texture2DArray textureArray;
    Vector3[] offArray;
    ComputeBuffer debugOffBuffer;
    ComputeBuffer offBuffer;
    RenderTexture projected;

    int project;
    uint projectThreadsX, projectThreadsY;
    int projectWidth, projectHeight;

    void Start()
    {
        string path = Path.Combine(renderPath, mapPath);
        map = new Map(path);

        projectWidth = geometryResolution.x;
        projectHeight = geometryResolution.y;

        AddDebugger("Debug");

        postProcessingMat = new Material(postProcessing);
        project = projectShader.FindKernel("Projection");
        projectShader.GetKernelThreadGroupSizes(project, out projectThreadsX, out projectThreadsY, out uint _);

        SetUpTextures();
        SetShaderConstants();
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
    }

    void OnDestroy()
    {
        if (offBuffer != null) offBuffer.Release();
        if (debugOffBuffer != null) debugOffBuffer.Release();
        if (projected != null) projected.Release();
    }


    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(projected, destination, postProcessingMat);
        
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
        projected.enableRandomWrite = true;
        projected.Create();
        offArray = new Vector3[maxTextures];
        debugOffArray = new Vector3[maxTextures];
        offBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
        debugOffBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
    }

    void SetShaderConstants()
    {
        projectShader.SetFloat("PI", Mathf.PI);
        projectShader.SetFloat("PI2", Mathf.PI * 2);
        projectShader.SetFloat("FCLIP", map.config.fclip);
        postProcessingMat.SetFloat("PI", Mathf.PI);
        postProcessingMat.SetFloat("PI2", Mathf.PI * 2);
        postProcessingMat.SetFloat("FCLIP", map.config.fclip);

        projectShader.SetBuffer(project, "OffsetBuffer", offBuffer);
        projectShader.SetBuffer(project, "DebugOffsetBuffer", debugOffBuffer);
        projectShader.SetTexture(project, "InputArray", textureArray);
        projectShader.SetTexture(project, "Projected", projected);

        postProcessingMat.SetTexture("InputArray", textureArray);
    }

    void SetComputeShaderValues()
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
}