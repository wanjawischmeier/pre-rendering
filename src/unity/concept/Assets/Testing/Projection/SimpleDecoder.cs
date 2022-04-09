using PreRendering;
using System.IO;
using UnityEngine;
using static ShaderManager;

public class SimpleDecoder : MonoBehaviour
{
    public string path;
    public string[] files;
    [Range(0, 2)]
    public int img, chn;
    public float lm;
    public Shader shader;
    public MapConfig config;
    public float geometryPercision = 0.75f;
    public ShaderDebugMode shaderDebugging = ShaderDebugMode.Disabled;
    public float depthOfField = 0;
    public float mistOffset = 1;
    public float mistFalloff = 0.1f;
    public Color mistColor = Color.white;
    public float pIcon = 0.05f;
    public Texture2D texture;
    Texture2DArray textures;
    Material material;
    Camera mainCamera;

    private void Start()
    {
        texture = new Texture2D(0, 0, TextureFormat.RGB24, false);

        string sampleFile = Path.Combine(path, files[0]);
        byte[] data = File.ReadAllBytes(sampleFile);
        texture.LoadImage(data);

        textures = new Texture2DArray(texture.width, texture.height, files.Length, texture.format, false);

        for (int i = 0; i < files.Length; i++)
        {
            string file = Path.Combine(path, files[i]);
            data = File.ReadAllBytes(file);
            texture.LoadImage(data);
            Graphics.CopyTexture(texture, 0, textures, i);
        }

        material = new Material(shader);
        material.SetTexture("_InputBuffer", textures);
        material.SetInt("IMG_IDX", 0);
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
        material.SetFloat("NegativeInfinity", float.NegativeInfinity);
        material.SetFloat("PositiveInfinity", float.PositiveInfinity);
        material.SetFloat("NCLIP", config.nclip);
        material.SetFloat("FCLIP", config.fclip);
        material.SetVector("InputBufferResolution", new Vector2(texture.width, texture.height));
        material.SetVector("POSITION_OFFSET", Vector3.zero);

        mainCamera = Camera.main;
    }

    private void Update()
    {
        material.SetInt("DEBUG", (int)shaderDebugging);
        material.SetInt("IMG_IDX", img);
        material.SetInt("CHANNEL", chn);
        material.SetFloat("FOV", mainCamera.fieldOfView * Mathf.Deg2Rad);
        material.SetFloat("LM", lm);
        material.SetFloat("PLAYER_ICON", pIcon);
        material.SetFloat("DOF_INTENSITY", depthOfField);
        material.SetFloat("MIST_FALLOFF", mistFalloff);
        material.SetFloat("MIST_OFFSET", mistOffset);
        material.SetVector("MIST_COLOR", mistColor);
        material.SetVector("POSITION", transform.position);
        material.SetVector("ROTATION", transform.eulerAngles * Mathf.Deg2Rad);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }
}
