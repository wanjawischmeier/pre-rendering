using UnityEngine;

public class DownhillSimplexAbstract : MonoBehaviour
{
    public Shader shader;
    public Texture2D input;
    public float fac, off;
    public Vector2 x0, x1, x2, tgt;

    private Material material;

    private void Start()
    {
        material = new Material(shader);
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
        material.SetTexture("_MainTex", input);
    }

    private void Update()
    {
        material.SetFloat("FAC", fac);
        material.SetFloat("OFF", off);
        material.SetVector("X0", x0);
        material.SetVector("X1", x1);
        material.SetVector("X2", x2);
        material.SetVector("TGT", tgt);
        material.SetVector("OFFSET", -transform.position);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }
}
