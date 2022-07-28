using UnityEngine;

public class DownhillSimplexDemo : MonoBehaviour
{
    public Shader shader;
    public Texture2D input;
    public float fac, off, triangleCentroidRadius;
    public Vector2 tgt;

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
        material.SetFloat("TRIANGLE_CENTROID_RADIUS", triangleCentroidRadius);
        material.SetVector("TGT", tgt);
        material.SetVector("OFFSET", -transform.position);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }
}
