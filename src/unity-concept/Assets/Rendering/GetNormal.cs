using UnityEngine;

public class GetNormal : MonoBehaviour
{
    public Shader shader;
    public Texture input;
    Material mat;

    void Start()
    {
        mat = new Material(shader);
        mat.SetVector("Resolution", new Vector2(input.width, input.height));
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(input, destination, mat);
    }
}
