using UnityEngine;
using MapManagement;
using System;
using UnityEngine.UI;

public class TextureLoader : MonoBehaviour
{
    public string mapBundle;
    public ComputeShader shader;
    public RawImage[] disp;
    [Range(5, 100)]
    public int maxTextures = 10;

    Map map;

    public Texture2D[] texture2s;
    public Texture2DArray textureArray;
    Vector3[] offArray;
    ComputeBuffer offBuffer;
    RenderTexture translated;
    RenderTexture result;

    int kernelTranslate, kernelProject;
    uint translateThreadsX, translateThreadsY;
    uint projectThreadsX, projectThreadsY;
    int screenWidth, screenHeight;

    void Start()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        maxTextures = disp.Length;

        map = new Map(mapBundle);

        GameObject.Find("FPS Counter (White)")
            .GetComponent<FPSCounter>()
            .textureResolution = new Vector2(
                map.config.textureWidth,
                map.config.textureHeight);

        kernelTranslate = shader.FindKernel("Translation");
        kernelProject = shader.FindKernel("GnomicProjection");
        shader.GetKernelThreadGroupSizes(kernelTranslate, out translateThreadsX, out translateThreadsY, out uint _);
        shader.GetKernelThreadGroupSizes(kernelProject, out projectThreadsX, out projectThreadsY, out uint _);

        textureArray = new Texture2DArray(map.config.textureWidth, map.config.textureHeight, maxTextures, TextureFormat.RGBA64, 1, false);
        translated = new RenderTexture(map.config.textureWidth, map.config.textureHeight, 24);
        result = new RenderTexture(screenWidth, screenHeight, 24);
        translated.enableRandomWrite = true;
        result.enableRandomWrite = true;
        translated.Create();
        result.Create();

        texture2s = new Texture2D[maxTextures];
        offArray = new Vector3[maxTextures];
        offBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
        offBuffer.SetData(offArray);

        shader.SetFloat("PI", Mathf.PI);
        shader.SetFloat("FCLIP", map.config.fclip);
        shader.SetBuffer(kernelTranslate, "OffsetBuffer", offBuffer);
        shader.SetTexture(kernelTranslate, "InputArray", textureArray);
        shader.SetTexture(kernelTranslate, "Translated", translated);
        shader.SetTexture(kernelProject, "Translated", translated);
        shader.SetTexture(kernelProject, "Result", result);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();

        shader.SetVector("Position", transform.position);
        shader.SetVector("Rotation", transform.eulerAngles * Mathf.Deg2Rad);
        shader.SetFloat("FOV", (180 - Camera.main.fieldOfView) * Mathf.Deg2Rad);
        // shader.SetBool("Debug", debug);

        offArray = map.GetClosest(transform.position, maxTextures);
        map.SetTexturesAtPositions(offArray, ref textureArray, ref texture2s);

        for (int i = 0; i < disp.Length; i++)
        {
            disp[i].texture = texture2s[i];
        }
        /*
        shader.Dispatch(kernelTranslate, map.config.textureWidth / (int)translateThreadsX, map.config.textureHeight / (int)translateThreadsY, maxTextures);
        shader.Dispatch(kernelProject, screenWidth / (int)projectThreadsX, screenHeight / (int)projectThreadsY, 1);

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = translated;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;
        */
    }

    void OnDestroy()
    {
        if (offBuffer != null) offBuffer.Release();
        if (translated != null) translated.Release();
        if (result != null) result.Release();
    }


    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination);
    }
}