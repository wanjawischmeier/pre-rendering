using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadImage : MonoBehaviour
{
    public Texture2D texture;
    public ComputeShader computeShader;
    public float nclip, fclip, dist, off, mul;
    public Vector2 viewDirection;

    private int kernel;
    private RenderTexture result;

    private void Start()
    {
        kernel = computeShader.FindKernel("CalculateNormals");
        result = new RenderTexture(texture.width, texture.height, 0);
        result.enableRandomWrite = true;

        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetFloat("PI2", Mathf.PI * 2);
        computeShader.SetVector("INPUT_RESOLUTION", new Vector2(texture.width, texture.height));
        computeShader.SetTexture(kernel, "Input", texture);
        computeShader.SetTexture(kernel, "Result", result);
    }

    private void Update()
    {
        computeShader.SetFloat("NCLIP", nclip);
        computeShader.SetFloat("FCLIP", fclip);
        computeShader.SetFloat("DIST", dist);
        computeShader.SetFloat("OFF", off);
        computeShader.SetFloat("MUL", mul);
        computeShader.SetVector("VIEW_DIR", viewDirection);
        computeShader.Dispatch(kernel, texture.width, texture.height, 1);
    }


    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(result, destination);
    }
}
