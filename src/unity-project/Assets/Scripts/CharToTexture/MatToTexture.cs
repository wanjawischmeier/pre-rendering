using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class MatToTexture : MonoBehaviour
{
    const string builddir1 = "E:\\users\\wanja\\Dokumente\\Programmieren\\C#\\pre-rendering\\";
    const string builddir2 = "C:\\Users\\wanja\\Documents\\dev\\csharp\\pre-rendering\\";
    const string devbuild = builddir2 + "src\\decoder\\cpp-decoder-class\\x64\\Debug\\Decoder.dll";
    // const string devbuild = "Assets/Plugins/Decoder.dll";

    [DllImport(devbuild, EntryPoint = "newDecoder")]
    public static extern int NewDecoder(string path);
    [DllImport(devbuild, EntryPoint = "setFrame")]
    public static extern bool SetFrame(int id, int frame);
    [DllImport(devbuild, EntryPoint = "getUnsignedBytes")]
    public static extern IntPtr GetUnsignedBytes(string path, out int bytes_count, bool debug = false);

    public string image_path;

    public Vector2Int resolution;
    public int channels = 3;
    public Texture2D texture;

    void Start()
    {
        IntPtr ptr = GetUnsignedBytes(image_path, out int bytes_count);

        byte[] bytes = new byte[bytes_count];
        Marshal.Copy(ptr, bytes, 0, bytes_count);

        texture = new Texture2D(resolution.x, resolution.y);

        Color32[] colorArray = new Color32[bytes.Length / channels];

        for (int i, x = 0; x < resolution.x; x++)
        {
            for (int y = 0; y < resolution.y; y++)
            {
                i = (x * channels) + (y * channels) * resolution.x;

                var color = new Color32(
                    bytes[i + 2], bytes[i + 1],
                    bytes[i + 0], 255 // bytes[i + 3]
                );

                colorArray[x + (resolution.y - y -1) * resolution.x] = color;
            }
        }

        texture.SetPixels32(colorArray);
        texture.Apply();

        File.WriteAllText("testfile.txt", "thisisjustatest");
    }

    void Update()
    {
        
    }
}
