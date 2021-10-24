using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Upscaling : MonoBehaviour
{
    public Shader shader;
    public Texture texLow;
    public RenderTexture texHigh;
    public Vector2Int highRes;

    void Start()
    {
        Material mat = new Material(shader);
        texHigh = new RenderTexture(highRes.x, highRes.y, 24);

        
        Graphics.Blit(texLow, texHigh);
    }
}
