using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadImage : MonoBehaviour
{
    public Texture2D texture;
    public ComputeShader computeShader;
    public float nclip, fclip;

    private int kernel;
    private RenderTexture result;

    private void Start()
    {
        kernel = computeShader.FindKernel("CalculateNormals");
        result = new RenderTexture(texture.width, texture.height, 0);
        result.enableRandomWrite = true;

        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetFloat("NCLIP", nclip);
        computeShader.SetFloat("FCLIP", fclip);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(texture.width, texture.height));
        computeShader.SetTexture(kernel, "Input", texture);
        computeShader.SetTexture(kernel, "Result", result);
        computeShader.Dispatch(kernel, texture.width, texture.height, 1);
    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
