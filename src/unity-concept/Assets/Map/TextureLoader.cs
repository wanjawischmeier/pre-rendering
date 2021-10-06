using UnityEngine;
using MapManagement;
using System.IO;

public class TextureLoader : MonoBehaviour
{
    public ComputeShader projectShader;
    public Shader postProcessing;
    public MovementController controller;

    public bool debug;
    Vector3[] debugOffArray;
    public int selectedId = 1;

    FPSCounter debugDisplay;
    public Map map;

    Material postProcessingMat;

    ComputeBuffer debugOffBuffer;
    ComputeBuffer offBuffer;
    public RenderTexture projected;

    int project, combine;
    uint projectThreadsX, projectThreadsY, combineThreadsX, combineThreadsY;
    int projectWidth, projectHeight;

    public struct StartupConfig
    {
        public string main_path;
        public string map_name;
        public int[] raw_geometry_resolution;
        public Vector2Int geometryResolution;
        public int layer_depth;
    }
    public StartupConfig config;

    void Start()
    {
        string configPath = Path.Combine(Application.dataPath, ".startConfig");
        string rawConfig = File.ReadAllText(configPath);
        config = JsonUtility.FromJson<StartupConfig>(rawConfig);
        config.geometryResolution = new Vector2Int(config.raw_geometry_resolution[0], config.raw_geometry_resolution[1]);

        string path = Path.Combine(config.main_path, config.map_name);
        map = new Map(path, config.layer_depth);
        
        projectWidth = config.geometryResolution.x;
        projectHeight = config.geometryResolution.y;

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
        
        for (int i = 0; i < config.layer_depth; i++)
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

        debugOffArray = new Vector3[config.layer_depth];
        offBuffer = new ComputeBuffer(config.layer_depth, sizeof(float) * 3);
        debugOffBuffer = new ComputeBuffer(config.layer_depth, sizeof(float) * 3);
    }

    void SetShaderConstants()
    {
        Shader.SetGlobalFloat("PI", Mathf.PI);
        Shader.SetGlobalFloat("PI2", Mathf.PI * 2);
        Shader.SetGlobalFloat("FCLIP", map.config.fclip);
        Shader.SetGlobalInt("MX_IDX", config.layer_depth);
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
        if (selectedId > config.layer_depth) selectedId = 1;
        if (selectedId < 1) selectedId = config.layer_depth;
    }

    public Resolution EstimatePanoramaResolution(int width, int height, float fov)
    {
        Resolution res = new Resolution();
        res.width = Mathf.RoundToInt(width * (360 / fov));
        res.height = Mathf.RoundToInt(height * (180 / fov));
        return res;
    }
}