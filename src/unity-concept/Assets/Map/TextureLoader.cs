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
    public MovementController controller;

    public string mainPath;
    public string mapName;
    public Vector2Int geometryResolution;
    [Range(1, 100)]
    public int layerDepth = 4;
    [Range(1, 100)]
    public int cacheSize = 10;
    public bool debug;

    Vector3[] debugOffArray;
    public int selectedId = 1;

#if UNITY_EDITOR
    public Vector3[] pending;
    public Vector3[] decoded;
#endif

    FPSCounter debugDisplay;
    public Map map;

    Material postProcessingMat;

    ComputeBuffer debugOffBuffer;
    ComputeBuffer offBuffer;
    public RenderTexture projected;

    int project, combine;
    uint projectThreadsX, projectThreadsY, combineThreadsX, combineThreadsY;
    int projectWidth, projectHeight;

#if !UNITY_EDITOR
    public struct StartupConfig
    {
        public string main_path;
        public string map_name;
        public int[] screen_resolution;
        public float geometry_percision;
        public int layer_depth;
        public int cache_size;
    }
    public StartupConfig config;
#endif

    void Start()
    {
#if !UNITY_EDITOR
        string configPath = Path.Combine(Application.dataPath, "start.config");
        string rawConfig = File.ReadAllText(configPath);
        config = JsonUtility.FromJson<StartupConfig>(rawConfig);

        Screen.SetResolution(config.screen_resolution[0], config.screen_resolution[1], true);

        mainPath = config.main_path;
        mapName = config.map_name;
        geometryResolution = new Vector2Int(
            Mathf.RoundToInt(Screen.width * config.geometry_percision),
            Mathf.RoundToInt(Screen.height * config.geometry_percision)
        );
        layerDepth = config.layer_depth;
        cacheSize = config.cache_size;

#endif
        string path = Path.Combine(mainPath, mapName);
        map = new Map(path, layerDepth, cacheSize);

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

#if UNITY_EDITOR
        pending = new Vector3[map.pending.Count];
        decoded = new Vector3[map.decoded.Count];

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
        
        for (int i = 0; i < layerDepth; i++)
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
        debugDisplay.loader = this;
    }

    void SetUpTextures()
    {
        Resolution res = EstimatePanoramaResolution(Screen.width, Screen.height, Camera.main.fieldOfView);
        projected = new RenderTexture(res.width, res.height, 0, RenderTextureFormat.ARGB64);
        projected.enableRandomWrite = true;
        projected.Create();

        debugOffArray = new Vector3[layerDepth];
        offBuffer = new ComputeBuffer(layerDepth, sizeof(float) * 3);
        debugOffBuffer = new ComputeBuffer(layerDepth, sizeof(float) * 3);
    }

    void SetShaderConstants()
    {
        Shader.SetGlobalFloat("PI", Mathf.PI);
        Shader.SetGlobalFloat("PI2", Mathf.PI * 2);
        Shader.SetGlobalFloat("FCLIP", map.config.fclip);
        Shader.SetGlobalInt("MX_IDX", layerDepth);
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
#if !UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
#endif
        if (Input.GetKeyDown(KeyCode.F2)) debugDisplay.Toggle();
        if (Input.GetKeyDown(KeyCode.F3)) debug = !debug;
        if (Input.mouseScrollDelta.y > 0) selectedId += 1;
        if (Input.mouseScrollDelta.y < 0) selectedId -= 1;
        if (selectedId > layerDepth) selectedId = 1;
        if (selectedId < 1) selectedId = layerDepth;
    }

    public Resolution EstimatePanoramaResolution(int width, int height, float fov)
    {
        Resolution res = new Resolution();
        res.width = Mathf.RoundToInt(width * (360 / fov));
        res.height = Mathf.RoundToInt(height * (180 / fov));
        return res;
    }
}