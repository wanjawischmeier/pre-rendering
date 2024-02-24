using UnityEngine;

[ExecuteInEditMode]
public class GlobalTextureSetter : MonoBehaviour
{
    public Texture2D[] cubemapFaces;
    public Texture2DArray textureArray;
    public Material[] materials;

    private void Start()
    {
        if (cubemapFaces == null || cubemapFaces.Length == 0)
        {
            return;
        }

        var sampleTexture = cubemapFaces[0];
        textureArray = new Texture2DArray(sampleTexture.width, sampleTexture.height, cubemapFaces.Length, sampleTexture.format, false);
        for (int i = 0; i < cubemapFaces.Length; i++)
        {
            Graphics.CopyTexture(cubemapFaces[i], 0, textureArray, i);
        }

        foreach (var material in materials)
        {
            // material.SetTexture("_InputCubemapFaces", textureArray);
        }
        Shader.SetGlobalTexture("_InputCubemapFaces", textureArray);
    }

    private void OnDestroy()
    {
        if (textureArray != null)
        {
            Destroy(textureArray);
        }
    }
}
