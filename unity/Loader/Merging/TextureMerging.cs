using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureMerging : MonoBehaviour
{
    public Texture2D input;
    public RenderTexture result;

    public ComputeShader shader;

    void Start()
    {
        result = new RenderTexture(input.width, input.height, 0);
        result.enableRandomWrite = true;

        shader.SetTexture(0, "Input", input);
        shader.SetTexture(0, "Result", result);

        shader.Dispatch(0, input.width / 8, input.height / 8, 1);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
