using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseTest : MonoBehaviour
{
    public ComputeShader computeShader;
    public RenderTexture renderTexture;

    [Range(1, 10)]
    public int threadGroupsX = 8;
    [Range(1, 10)]
    public int threadGroupsY = 8;
    [Range(1, 10)]
    public int threadGroupsZ = 1;

    public Vector3 off;

    void Start()
    {
        renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        computeShader.SetVector("Resolution", new Vector4(Screen.width, Screen.height, 0, 0));
    }

    void Update()
    {
        
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.Dispatch(0, renderTexture.width / threadGroupsX, renderTexture.height / threadGroupsY, threadGroupsZ);

        Graphics.Blit(renderTexture, destination);
    }
}
