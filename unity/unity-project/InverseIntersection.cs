using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InverseIntersection : MonoBehaviour
{
    public Texture tex_xp;
    public Texture tex_xn;
    public Texture tex_yp;
    public Texture tex_yn;

    public ComputeShader computeShader;
    public RenderTexture translated;
    public RenderTexture result;

    public Vector3 center;
    public Vector3 positionXP;
    public Vector3 positionXN;
    public Vector3 positionYP;
    public Vector3 positionYN;
    public Vector2 rotation;

    int csmain, test;
    int tex_width, tex_height;

    void Start()
    {
        tex_width = tex_xp.width;
        tex_height = tex_xp.height;

        csmain = computeShader.FindKernel("CSMain");
        test = computeShader.FindKernel("Test");

        computeShader.SetFloat("PI", Mathf.PI);
        computeShader.SetVector("Resolution", new Vector2(tex_width, tex_height));
    }

    void Update()
    {
        computeShader.SetFloat("Latitude", rotation.x);
        computeShader.SetFloat("Longitude", rotation.y);
        computeShader.SetVector("C", center);
        computeShader.SetVector("V_XP", positionXP);
        computeShader.SetVector("V_XN", positionXN);
        computeShader.SetVector("V_YP", positionYP);
        computeShader.SetVector("V_YN", positionYN);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        translated = new RenderTexture(tex_width, tex_height, 24);
        result = new RenderTexture(tex_width, tex_height, 24);
        translated.enableRandomWrite = true;
        result.enableRandomWrite = true;
        translated.Create();
        result.Create();

        computeShader.SetTexture(csmain, "Input_XP", tex_xp);
        computeShader.SetTexture(csmain, "Input_XN", tex_xn);
        computeShader.SetTexture(csmain, "Input_YP", tex_yp);
        computeShader.SetTexture(csmain, "Input_YN", tex_yn);
        computeShader.SetTexture(csmain, "Translated", translated);
        computeShader.Dispatch(csmain, tex_width / 8, tex_height / 8, 1);

        computeShader.SetTexture(test, "TranslatedIn", translated);
        computeShader.SetTexture(test, "Result", result);
        computeShader.Dispatch(test, tex_width / 8, tex_height / 8, 1);

        Graphics.Blit(result, destination);
        translated.Release();
        result.Release();
    }
}
