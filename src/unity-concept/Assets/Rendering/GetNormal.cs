using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetNormal : MonoBehaviour
{
    public Shader shader;
    public Texture input;
    public RenderTexture output;

    void Start()
    {
        output = new RenderTexture(input.width, input.height, 0);

        Graphics.Blit(input, output, new Material(shader));
    }
}
