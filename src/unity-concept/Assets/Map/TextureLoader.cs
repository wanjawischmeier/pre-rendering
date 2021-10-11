using UnityEngine;
using MapManagement;
using System.IO;
using System;
using UnityEngine.Networking;
using System.Collections.Generic;

public class TextureLoader : MonoBehaviour
{
    public ComputeShader projectShader;
    public Shader postProcessing;
    [HideInInspector]
    public MovementController controller;

    public string mainPath;
    public string mapName;
    public Vector2Int geometryResolution;
    [Range(1, 100)]
    public int layerDepth = 4;
    [Range(1, 100)]
    public int cacheSize = 10;
    public bool debug;
    public float l;

    Vector3[] debugOffArray;
    public int selectedId = 1;

#if UNITY_EDITOR
    public Vector3[] pending;
    public Vector3[] decoded;
    public Vector3[] off;
#endif

    public Map map;

    Material postProcessingMat;

    ComputeBuffer debugOffBuffer;
    ComputeBuffer offBuffer;
    public RenderTexture projected;

    int project, combine;
    uint projectThreadsX, projectThreadsY, combineThreadsX, combineThreadsY;

    void Start()
    {
        string path = Path.Combine(mainPath, mapName);
        map = new Map(path, cacheSize);

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
        SetShaderValues();

        map.LoadTexturesNearPosition(transform.position, layerDepth);

#if UNITY_EDITOR
        pending = new Vector3[map.pending.Count];
        decoded = new Vector3[map.decoded.Count];
        off = map.offArray;

        int idx = 0;
        foreach (KeyValuePair<AsyncOperation, Tuple<Vector3, UnityWebRequest>> item in map.pending)
            pending[idx] = map.pending[item.Key].Item1; idx++;

        idx = 0;
        foreach (KeyValuePair<Vector3, UnityWebRequest> item in map.decoded)
            decoded[idx] = item.Key; idx++;
#endif

        debugOffArray[selectedId - 1] = controller.secondaryPosition;
        offBuffer.SetData(map.offArray);
        debugOffBuffer.SetData(debugOffArray);
        
        for (int i = layerDepth -1; i >= 0; i--) // Furthest away first, closest last (depth sorting)
        {
            float distance = Vector3.Distance(transform.position, map.offArray[i]);
            // TODO: Resolution based on distance
            int projectWidth = geometryResolution.x;
            int projectHeight = geometryResolution.y;

            RenderTexture rt = RenderTexture.GetTemporary(projectWidth, projectHeight, 0, RenderTextureFormat.ARGB64);
            rt.enableRandomWrite = true;

            Shader.SetGlobalVector("ProjectionRes", new Vector2(projectWidth, projectHeight));
            projectShader.SetInt("IMG_IDX", i);
            projectShader.SetTexture(project, "_Result", rt);
            projectShader.SetTexture(combine, "_Input", rt);

            projectShader.Dispatch(project, projectWidth / (int)projectThreadsX, projectHeight / (int)projectThreadsY, 1);
            projectShader.Dispatch(combine, projected.width / (int)combineThreadsX, projected.height / (int)combineThreadsY, 1);

            RenderTexture.ReleaseTemporary(rt);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(projected, destination, postProcessingMat);

        RenderTexture tmp = RenderTexture.active;
        RenderTexture.active = projected;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = tmp;
    }

    void OnDestroy()
    {
        if (offBuffer != null) offBuffer.Release();
        if (debugOffBuffer != null) debugOffBuffer.Release();
        if (projected != null) projected.Release();
    }

    void SetUpTextures()
    {
        Resolution res = EstimatePanoramaResolution(Screen.width, Screen.height, Camera.main.fieldOfView);
        projected = new RenderTexture(res.width, res.height, 0, RenderTextureFormat.ARGB64)
        {
            enableRandomWrite = true
        };
        projected.Create();

        debugOffArray = new Vector3[cacheSize];
        offBuffer = new ComputeBuffer(cacheSize, sizeof(float) * 3);
        debugOffBuffer = new ComputeBuffer(cacheSize, sizeof(float) * 3);
    }

    void SetShaderConstants()
    {
        Shader.SetGlobalFloat("PI", Mathf.PI);
        Shader.SetGlobalFloat("PI2", Mathf.PI * 2);
        Shader.SetGlobalFloat("FCLIP", map.config.fclip);
        Shader.SetGlobalInt("MX_IDX", layerDepth);
        Shader.SetGlobalVector("InputArrayRes", new Vector2(map.textures.width, map.textures.height));
        Shader.SetGlobalVector("ProjectedRes", new Vector2(projected.width, projected.height));
        Shader.SetGlobalTexture("_InputArray", map.textures);
        Shader.SetGlobalTexture("_Projected", projected);

        projectShader.SetBuffer(project, "OffsetBuffer", offBuffer);
        projectShader.SetBuffer(project, "DebugOffsetBuffer", debugOffBuffer);
    }

    void SetShaderValues()
    {
        projectShader.SetVector("Position", transform.position);
        projectShader.SetFloat("L", l);
        postProcessingMat.SetVector("Rotation", transform.eulerAngles * Mathf.Deg2Rad);
        postProcessingMat.SetFloat("FOV", (180 - Camera.main.fieldOfView) * Mathf.Deg2Rad);
        postProcessingMat.SetInt("Debug", debug ? 1 : 0);
    }

    public Resolution EstimatePanoramaResolution(int width, int height, float fov)
    {
        Resolution res = new Resolution
        {
            width = Mathf.RoundToInt(width * (360 / fov)),
            height = Mathf.RoundToInt(height * (180 / fov))
        };
        return res;
    }
}