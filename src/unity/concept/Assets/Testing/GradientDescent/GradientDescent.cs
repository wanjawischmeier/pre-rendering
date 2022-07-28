using UnityEngine;

public class GradientDescent : MonoBehaviour
{
    public Shader shader;
    public Texture2D input;
    public float fac, off, learningRate, adaptiveLearning;
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
        material.SetFloat("LEARNING_RATE", learningRate);
        material.SetFloat("ADAPTIVE_LEARNING", adaptiveLearning);
        material.SetVector("TGT", tgt);
        material.SetVector("OFFSET", -transform.position);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }
}
