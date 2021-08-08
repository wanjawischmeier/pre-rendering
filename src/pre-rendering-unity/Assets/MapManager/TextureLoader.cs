using UnityEngine;
using MapManagement;
using System;

public class TextureLoader : MonoBehaviour
{
    public string mapBundle;
    public ComputeShader shader;
    public MovementController controller;
    [Range(1, 100)]
    public int maxTextures = 10;
    public Vector2Int geometryResolution;
    public bool debug;
    public bool fill;
    public float fillOff;
    public Vector3[] debugOffArray;
    public int selectedId = 1;

    FPSCounter debugDisplay;
    Map map;

    Texture2DArray textureArray;
    Vector3[] offArray;
    ComputeBuffer debugOffBuffer;
    ComputeBuffer offBuffer;
    RenderTexture projected;
    RenderTexture result;

    int project, gnomonic;
    uint projectThreadsX, projectThreadsY;
    uint gnomonicThreadsX, gnomonicThreadsY;
    int screenWidth, screenHeight;
    int projectWidth, projectHeight;

    void Start()
    {
        map = new Map(mapBundle);

        screenWidth = Screen.width;
        screenHeight = Screen.height;
        projectWidth = geometryResolution.x;
        projectHeight = geometryResolution.y;

        debugDisplay = GameObject.Find("Debug").GetComponent<FPSCounter>();
        debugDisplay.selected = selectedId;
        debugDisplay.maxTextures = maxTextures;
        debugDisplay.textureResolution = new Vector2(
            map.config.textureWidth,
            map.config.textureHeight);

        project = shader.FindKernel("Projection");
        gnomonic = shader.FindKernel("Gnomonic");
        shader.GetKernelThreadGroupSizes(project, out projectThreadsX, out projectThreadsY, out uint _);
        shader.GetKernelThreadGroupSizes(gnomonic, out gnomonicThreadsX, out gnomonicThreadsY, out uint _);

        textureArray = new Texture2DArray(map.config.textureWidth, map.config.textureHeight, maxTextures, TextureFormat.RGBA64, 1, false);
        projected = new RenderTexture(projectWidth, projectHeight, 24);
        result = new RenderTexture(screenWidth, screenHeight, 24);
        projected.enableRandomWrite = true;
        result.enableRandomWrite = true;
        projected.Create();
        result.Create();
        offArray = new Vector3[maxTextures];
        debugOffArray = new Vector3[maxTextures];
        offBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
        debugOffBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
        
        shader.SetFloat("PI", Mathf.PI);
        shader.SetFloat("PI2", Mathf.PI * 2);
        shader.SetFloat("FCLIP", map.config.fclip);
        shader.SetFloat("FOV", (180 - Camera.main.fieldOfView) * Mathf.Deg2Rad);
        shader.SetBuffer(project, "OffsetBuffer", offBuffer);
        shader.SetBuffer(project, "DebugOffsetBuffer", debugOffBuffer);
        shader.SetTexture(project, "InputArray", textureArray);
        shader.SetTexture(gnomonic, "InputArray", textureArray);
        shader.SetTexture(project, "Projected", projected);
        shader.SetTexture(gnomonic, "ProjectedIn", projected);
        shader.SetTexture(gnomonic, "Result", result);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
        if (Input.GetKeyDown(KeyCode.F3)) debug = !debug;
        if (Input.GetKeyDown(KeyCode.F4)) fill = !fill;
        if (Input.mouseScrollDelta.y > 0) selectedId += 1;
        if (Input.mouseScrollDelta.y < 0) selectedId -= 1;
        if (selectedId > maxTextures) selectedId = 1;
        if (selectedId < 1) selectedId = maxTextures;
        debugDisplay.selected = selectedId;

        shader.SetVector("Position", transform.position);
        shader.SetVector("Rotation", transform.eulerAngles * Mathf.Deg2Rad);
        shader.SetFloat("Off", fillOff);
        shader.SetBool("Debug", debug);
        
        offArray = map.GetClosest(transform.position, maxTextures);
        debugOffArray[selectedId -1] = controller.secondaryPosition;
        offBuffer.SetData(offArray);
        debugOffBuffer.SetData(debugOffArray);

        map.SetTexturesAtPositions(offArray, ref textureArray);

        shader.Dispatch(project, projectWidth / (int)projectThreadsX, projectHeight / (int)projectThreadsY, maxTextures);
        shader.Dispatch(gnomonic, screenWidth / (int)gnomonicThreadsX, screenHeight / (int)gnomonicThreadsY, 1);
    }

    void OnDestroy()
    {
        if (offBuffer != null) offBuffer.Release();
        if (debugOffBuffer != null) debugOffBuffer.Release();
        if (result != null) result.Release();
    }


    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
        
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = projected;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;
    }
}