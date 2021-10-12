using UnityEngine;

[ExecuteInEditMode]
public class Merge : MonoBehaviour
{
    public Shader merge;
    public Shader seperate;
    public Texture maps;
    public RenderTexture target;
    public RenderTexture result;
    Material mergeMat;
    Material seperateMat;



    void OnValidate()
    {
        mergeMat = new Material(merge);
        seperateMat = new Material(seperate);
        target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64);
        result = new RenderTexture(target);
    }

    void Update()
    {
        Graphics.Blit(maps, target, mergeMat);
        Graphics.Blit(target, result, seperateMat);
    }
}
