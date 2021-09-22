using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class DecoderWrapper : MonoBehaviour
{
    const string projectpath1 = "S:\\users\\wanja\\Dokumente";
    const string projectpath2 = "C:\\Users\\wanja\\Documents\\dev\\csharp";
    const string decoderdll = projectpath1 + "\\pre-rendering\\master\\src\\image-decoder\\x64\\Debug\\Decoder.dll";
    
    [DllImport(decoderdll)]
    public static extern IntPtr GetUnsignedBytes(string path, out int bytes_count, bool debug = false);

    public string image_path;

    public Vector2Int resolution;
    public int channels = 3;
    public Texture2D texture;

    void Start()
    {
        Debug.Log("Starting...");

        IntPtr ptr = GetUnsignedBytes(image_path, out int bytes_count);
        byte[] bytes = new byte[bytes_count];
        Marshal.Copy(ptr, bytes, 0, bytes_count);

        Debug.Log(bytes.Length);
        Debug.Log("Ended");

        texture = new Texture2D(resolution.x, resolution.y);

        Color32[] colorArray = new Color32[bytes.Length / channels];

        for (int i, x = 0; x < resolution.x; x++)
        {
            for (int y = 0; y < resolution.y; y++)
            {
                i = (x * channels) + (y * channels) * resolution.x;

                var color = new Color32(
                    bytes[i + 0], bytes[i + 1],
                    bytes[i + 2], 255 // bytes[i + 3]
                );

                colorArray[i / channels] = color;
            }
        }

        texture.SetPixels32(colorArray);
        texture.Apply();

        File.WriteAllBytes(Application.dataPath + "\\outimg.png", texture.EncodeToPNG());
    }
}
