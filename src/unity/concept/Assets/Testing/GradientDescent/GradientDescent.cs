using UnityEngine;

public class GradientDescent : MonoBehaviour
{
    public Shader shader;
    public Texture2D input;
    public float f;

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
        material.SetFloat("F", f);
        material.SetVector("POSITION", transform.position);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }
}
