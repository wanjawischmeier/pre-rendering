using UnityEngine;

public class NormalBasedProjection : MonoBehaviour
{
    public Texture2D texture;
    public Shader shader;
    public float dist;

    Material material;

    private void Start()
    {
        material = new Material(shader);
        material.mainTexture = texture;
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
    }

    private void Update()
    {
        material.SetVector("POSITION", transform.position);
        material.SetFloat("XDIST", dist / texture.width);
        material.SetFloat("YDIST", dist / texture.height);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }
}
