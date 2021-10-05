using UnityEngine;

public class Merge : MonoBehaviour
{
    public Shader merge;
    public Shader seperate;
    public Texture maps;
    public RenderTexture target;
    public RenderTexture result;
    Material mergeMat;
    Material seperateMat;



    void Start()
    {
        mergeMat = new Material(merge);
        seperateMat = new Material(seperate);
        target = new RenderTexture(maps.width, maps.height, 0);
        result = new RenderTexture(target);

        Graphics.Blit(maps, target, mergeMat);
        Graphics.Blit(target, result, seperateMat);
    }
}
