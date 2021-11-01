using System.IO;
using UnityEngine;

public class LoadTextureToMat : MonoBehaviour
{
    public string path;
    public Material material;

    private void Start()
    {
        Texture2D texture = new Texture2D(0, 0);
        texture.LoadImage(File.ReadAllBytes(path));
        Texture2D high = new Texture2D(texture.width, texture.height, TextureFormat.RGBA64, false);
        high.SetPixels(texture.GetPixels());
        high.Apply();

        material.SetTexture("_MainTex", high);
    }
}
