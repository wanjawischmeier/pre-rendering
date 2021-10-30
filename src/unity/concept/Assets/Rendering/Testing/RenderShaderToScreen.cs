using UnityEngine;

[ExecuteInEditMode]
public class RenderShaderToScreen : MonoBehaviour
{
    public Shader shader;
    public Texture[] textures;
    Material mat;

    void Start()
    {
        mat = new Material(shader);
        mat.SetFloat("PI", Mathf.PI);

        if (textures.Length > 0)
        {
            mat.SetVector("Resolution", new Vector2(textures[0].width, textures[0].height));

            foreach (Texture texture in textures)
            {
                string texName = string.Format("_{0}Tex", texture.name);
                mat.SetTexture(texName, texture);
            }
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (textures.Length > 0)
            Graphics.Blit(textures[0], destination, mat);
        else
            Graphics.Blit(null, destination, mat);
    }
}
