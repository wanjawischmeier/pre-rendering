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
    public bool debug;
    public bool fill;
    public float fillOff;
    public Vector3[] debugOffArray;
    public int selectedId;

    FPSCounter debugDisplay;
    Map map;

    Texture2DArray textureArray;
    Vector3[] offArray;
    ComputeBuffer debugOffBuffer;
    ComputeBuffer offBuffer;
    RenderTexture CONV_SCREEN_LATLON;
    RenderTexture CONV_LATLON_SCREEN;
    RenderTexture result;
    RenderTexture final;

    int project, setConvMaps;
    uint convThreadsX, convThreadsY;
    uint projectThreadsX, projectThreadsY;
    int screenWidth, screenHeight;
    int projectWidth, projectHeight;

    void Start()
    {
        map = new Map(mapBundle);

        screenWidth = Screen.width;
        screenHeight = Screen.height;
        projectWidth = map.config.textureWidth;
        projectHeight = map.config.textureHeight;
        selectedId = 1;

        debugDisplay = GameObject.Find("Debug").GetComponent<FPSCounter>();
        debugDisplay.selected = selectedId;
        debugDisplay.maxTextures = maxTextures;
        debugDisplay.textureResolution = new Vector2(
            map.config.textureWidth,
            map.config.textureHeight);

        setConvMaps = shader.FindKernel("SetConvMaps");
        project = shader.FindKernel("Projection");
        shader.GetKernelThreadGroupSizes(setConvMaps, out convThreadsX, out convThreadsY, out uint _);
        shader.GetKernelThreadGroupSizes(project, out projectThreadsX, out projectThreadsY, out uint _);

        textureArray = new Texture2DArray(map.config.textureWidth, map.config.textureHeight, maxTextures, TextureFormat.RGBA64, 1, false);
        CONV_SCREEN_LATLON = new RenderTexture(projectWidth, projectHeight, 24, RenderTextureFormat.RG32);
        CONV_LATLON_SCREEN = new RenderTexture(projectWidth, projectHeight, 24, RenderTextureFormat.RG32);
        result = new RenderTexture(projectWidth, projectHeight, 24);
        final = new RenderTexture(projectWidth, projectHeight, 24);
        CONV_SCREEN_LATLON.enableRandomWrite = true;
        CONV_LATLON_SCREEN.enableRandomWrite = true;
        result.enableRandomWrite = true;
        final.enableRandomWrite = true;
        CONV_SCREEN_LATLON.Create();
        CONV_LATLON_SCREEN.Create();
        result.Create();
        final.Create();

        offArray = new Vector3[maxTextures];
        debugOffArray = new Vector3[maxTextures];
        offBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
        debugOffBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);

        shader.SetFloat("PI", Mathf.PI);
        shader.SetFloat("PI2", Mathf.PI * 2);
        shader.SetFloat("FCLIP", map.config.fclip);
        shader.SetTexture(setConvMaps, "CONV_SCREEN_LATLON", CONV_SCREEN_LATLON);
        shader.SetTexture(setConvMaps, "CONV_LATLON_SCREEN", CONV_LATLON_SCREEN);
        shader.SetTexture(project, "CONV_SCREEN_LATLON", CONV_SCREEN_LATLON);
        shader.SetTexture(project, "CONV_LATLON_SCREEN", CONV_LATLON_SCREEN);
        shader.SetBuffer(project, "OffsetBuffer", offBuffer);
        shader.SetBuffer(project, "DebugOffsetBuffer", debugOffBuffer);
        shader.SetTexture(project, "InputArray", textureArray);
        shader.SetTexture(project, "Result", result);
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
        shader.SetFloat("FOV", (180 - Camera.main.fieldOfView) * Mathf.Deg2Rad);
        shader.SetFloat("Off", fillOff);
        shader.SetBool("Debug", debug);
        shader.SetBool("Fill", fill);
        
        offArray = map.GetClosest(transform.position, maxTextures);
        debugOffArray[selectedId -1] = controller.secondaryPosition;
        offBuffer.SetData(offArray);
        debugOffBuffer.SetData(debugOffArray);

        map.SetTexturesAtPositions(offArray, ref textureArray);

        // shader.Dispatch(setgnomonicMaps, projectWidth / (int)convThreadsX, projectHeight / (int)convThreadsY, 1);
        shader.Dispatch(project, projectWidth / (int)projectThreadsX, projectWidth / (int)projectThreadsY, maxTextures);
        // shader.Dispatch(gnomonicProjection, projectWidth / (int)convThreadsX, projectWidth / (int)convThreadsY, maxTextures);
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
        RenderTexture.active = final;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;
    }
}