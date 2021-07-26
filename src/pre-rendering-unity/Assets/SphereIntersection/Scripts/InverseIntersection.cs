using System.IO;
using System.Linq;
using UnityEngine;

public class InverseIntersection : MonoBehaviour
{
    public ComputeShader computeShader;
    public string texPath;
    
    Texture2D[] rawTexArray;
    Texture2DArray texArray;

    Vector3[] offArray;
    ComputeBuffer offBuffer;
    float fClip = 10;

    RenderTexture translated;
    RenderTexture result;

    public bool debug;

    int kernelTranslate, kernelProject;
    uint translateThreadsX, translateThreadsY;
    uint projectThreadsX, projectThreadsY;
    int textureWidth, textureHeight, screenWidth, screenHeight;

    void Start()
    {
        if (!File.Exists(Application.dataPath + "\\DataPath.txt")) return;
        texPath = File.ReadAllText(Application.dataPath + "\\DataPath.txt");

        string[] imageFiles = Directory.GetFiles(texPath, "*.png");
        rawTexArray = new Texture2D[imageFiles.Length];
        offArray = new Vector3[imageFiles.Length];

        for (int i = 0; i < imageFiles.Length; i++)
        {
            rawTexArray[i] = new Texture2D(0, 0);
            rawTexArray[i].LoadImage(File.ReadAllBytes(imageFiles[i]));

            string[] split = Path.GetFileNameWithoutExtension(imageFiles[i])
                .Split('_');
            offArray[i] = new Vector3(
                float.Parse(split[0]),
                float.Parse(split[1]),
                float.Parse(split[2]));
        }

        textureWidth = rawTexArray[0].width;
        textureHeight = rawTexArray[0].height;
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        GameObject.Find("FPS Counter (White)")
            .GetComponent<FPSCounter>()
            .textureResolution = new Vector2(
                rawTexArray[0].width, 
                rawTexArray[0].height);

        kernelProject = computeShader.FindKernel("GnomicProjection");
        kernelTranslate = computeShader.FindKernel("Translation");
        computeShader.GetKernelThreadGroupSizes(kernelTranslate, out translateThreadsX, out translateThreadsY, out uint _);
        computeShader.GetKernelThreadGroupSizes(kernelProject, out projectThreadsX, out projectThreadsY, out uint _);

        texArray = new Texture2DArray(textureWidth, textureHeight, rawTexArray.Length, rawTexArray[0].format, false);
        translated = new RenderTexture(textureWidth, textureHeight, 24);
        result = new RenderTexture(screenWidth, screenHeight, 24);
        translated.enableRandomWrite = true;
        result.enableRandomWrite = true;
        translated.Create();
        result.Create();
        
        offBuffer = new ComputeBuffer(offArray.Length, sizeof(float) * 3);
        offBuffer.SetData(offArray);

        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("FCLIP", fClip);
        computeShader.SetBuffer(kernelTranslate, "OffsetBuffer", offBuffer);
        computeShader.SetTexture(kernelTranslate, "InputArray", texArray);
        computeShader.SetTexture(kernelTranslate, "Translated", translated);
        computeShader.SetTexture(kernelProject, "Translated", translated);
        computeShader.SetTexture(kernelProject, "Result", result);

        for (int i = 0; i < rawTexArray.Length; i++)
        {
            Graphics.CopyTexture(rawTexArray[i], 0, 0, texArray, i, 0);
        }

        offBuffer.SetData(offArray);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();

        computeShader.SetVector("Position", transform.position);
        computeShader.SetVector("Rotation", transform.eulerAngles * Mathf.Deg2Rad);
        computeShader.SetFloat("FOV", (180 - Camera.main.fieldOfView) * Mathf.Deg2Rad);
        computeShader.SetBool("Debug", debug);


        computeShader.Dispatch(kernelTranslate, textureWidth / (int)translateThreadsX, textureHeight / (int)translateThreadsY, rawTexArray.Length);
        computeShader.Dispatch(kernelProject, screenWidth / (int)projectThreadsX, screenHeight / (int)projectThreadsY, 1);

        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = translated;
        GL.Clear(false, true, Color.clear);
        RenderTexture.active = rt;
    }

    void OnDestroy()
    {
        if (offBuffer != null)  offBuffer.Release();
        if (translated != null) translated.Release();
        if (result != null)     result.Release();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}