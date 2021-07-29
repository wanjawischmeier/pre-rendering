using UnityEngine;

public class BufferManager : MonoBehaviour
{
    public ComputeShader computeShader;
    
    public Texture2DArray texArray;

    public Vector3[] offArray;
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
        textureWidth = texArray.width;
        textureHeight = texArray.height;
        screenWidth = Screen.width;
        screenHeight = Screen.height;

        GameObject.Find("FPS Counter (White)")
            .GetComponent<FPSCounter>()
            .textureResolution = new Vector2(
                texArray.width, 
                texArray.height);
        
        kernelProject = computeShader.FindKernel("GnomicProjection");
        kernelTranslate = computeShader.FindKernel("Translation");
        computeShader.GetKernelThreadGroupSizes(kernelTranslate, out translateThreadsX, out translateThreadsY, out uint _);
        computeShader.GetKernelThreadGroupSizes(kernelProject, out projectThreadsX, out projectThreadsY, out uint _);

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
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();

        computeShader.SetVector("Position", transform.position);
        computeShader.SetVector("Rotation", transform.eulerAngles * Mathf.Deg2Rad);
        computeShader.SetFloat("FOV", (180 - Camera.main.fieldOfView) * Mathf.Deg2Rad);
        computeShader.SetBool("Debug", debug);


        computeShader.Dispatch(kernelTranslate, textureWidth / (int)translateThreadsX, textureHeight / (int)translateThreadsY, texArray.depth);
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