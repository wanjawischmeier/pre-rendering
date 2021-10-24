using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helper;

public class Blitting : MonoBehaviour
{
    public Texture2D a;
    public Texture2DArray b;

    void Start()
    {
        b = new Texture2DArray(1000, 1000, 10, TextureFormat.ARGB32, false);

        a.CopyToArray(b, 4);
    }
}