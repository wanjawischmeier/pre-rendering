using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class MatToTexture : MonoBehaviour
{
    const string projectpath1 = "E:\\users\\wanja\\Dokumente\\Programmieren\\C#\\pre-rendering\\";
    const string projectpath2 = "C:\\Users\\wanja\\Documents\\dev\\csharp\\pre-rendering\\";
    const string decoderdll = projectpath2 + "src\\decoder\\cpp-decoder-class\\x64\\Debug\\Decoder.dll";

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
        /*
        for (var i = 0; i < bytes.Length; i += channels)
        {
            var color = new Color32(
                bytes[i + 2], bytes[i + 1],
                bytes[i + 0], 255 // bytes[i + 3]
            );

            colorArray[i / channels] = color;
        }
        */
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

        File.WriteAllBytes(Application.dataPath + "\\outimg4.png", texture.EncodeToPNG());
    }

    void Update()
    {
        
    }
}
