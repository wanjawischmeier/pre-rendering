using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MapManagement;
using System;

public class TextureLoader : MonoBehaviour
{
    public AssetBundle mapBundle;
    public ComputeShader shader;
    [Range(5, 100)]
    public int maxTextures = 10;

    Map map;

    Texture2DArray textureArray;
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

        map = new Map(mapBundle);
        map.FrameReady += OnFrameReady;

        GameObject.Find("FPS Counter (White)")
            .GetComponent<FPSCounter>()
            .textureResolution = new Vector2(
                map.textureWidth,
                map.textureHeight);

        kernelProject = shader.FindKernel("GnomicProjection");
        kernelTranslate = shader.FindKernel("Translation");
        shader.GetKernelThreadGroupSizes(kernelTranslate, out translateThreadsX, out translateThreadsY, out uint _);
        shader.GetKernelThreadGroupSizes(kernelProject, out projectThreadsX, out projectThreadsY, out uint _);

        textureArray = new Texture2DArray(map.textureWidth, map.textureHeight, maxTextures, map.textureFormat, false);
        translated = new RenderTexture(map.textureWidth, map.textureHeight, 24);
        result = new RenderTexture(screenWidth, screenHeight, 24);
        translated.enableRandomWrite = true;
        result.enableRandomWrite = true;
        translated.Create();
        result.Create();

        offArray = new Vector3[maxTextures];
        offBuffer = new ComputeBuffer(maxTextures, sizeof(float) * 3);
        offBuffer.SetData(offArray);

        shader.SetFloat("PI", Mathf.PI);
        shader.SetFloat("FCLIP", map.fClip);
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
    }

    void OnFrameReady(Texture2D frame, Vector3 offset)
    {
        int index = GetAvailableFrameIndex();
        Graphics.CopyTexture(frame, 0, 0, textureArray, index, 0);
        offArray[index] = offset;
    }

    int GetAvailableFrameIndex()
    {
        Vector3[] sorted = offArray.OrderBy(x => Vector3.Distance(transform.position, x)).ToArray();
        Vector3 available = sorted[sorted.Length - 1];
        return Array.IndexOf(offArray, available);
    }
}