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
    [DllImport(devbuild, EntryPoint = "getFrame")]
    public static extern IntPtr GetFrame(int id, int frame, out int bytes_count);
    [DllImport(devbuild, EntryPoint = "getUnsignedBytes")]
    public static extern IntPtr GetUnsignedBytes(string path, out int bytes_count, bool debug = false);

    public string image_path;

    public Vector2Int resolution;
    public int channels = 3;
    public Texture2D texture;

    void Start()
    {
        IntPtr ptr = GetUnsignedBytes(image_path, out int bytes_count);

        ptr.ToTexture2D(Utility.Image.Presets.FULL_HD);
    }

    void Update()
    {
        
    }
}

public static class Utility
{
    public struct Image
    {
        public struct Dimensions
        {
            public int width; public int height; public int channels;
            public int total
            {
                get { return this.width * this.height * this.channels; }
            }
        }

        public struct Presets
        {
            public static Dimensions FULL_HD =          new Dimensions() { width = 1920, height = 1080, channels = 3 };
            public static Dimensions FULL_HD_ALPHA =    new Dimensions() { width = 1920, height = 1080, channels = 4 };
            public static Dimensions HD =               new Dimensions() { width = 1280, height = 720,  channels = 3 };
            public static Dimensions HD_ALPHA =         new Dimensions() { width = 1280, height = 720,  channels = 4 };
        }
        
    }

    public static Texture2D ToTexture2D(this IntPtr ptr, Image.Dimensions resolution)
    {
        byte[] bytes = new byte[resolution.total];
        Marshal.Copy(ptr, bytes, 0, resolution.total);
        
        Texture2D texture = new Texture2D(resolution.width, resolution.height);

        Color32[] colorArray = new Color32[bytes.Length / resolution.channels];

        for (int i, x = 0; x < resolution.width; x++)
        {
            for (int y = 0; y < resolution.height; y++)
            {
                i = (x * resolution.channels) + (y * resolution.channels) * resolution.width;

                var color = new Color32(
                    bytes[i + 2], bytes[i + 1],
                    bytes[i + 0], 255 // bytes[i + 3]
                );

                colorArray[x + (resolution.height - y - 1) * resolution.width] = color;
            }
        }

        texture.SetPixels32(colorArray);
        texture.Apply();

        return texture;
    }
}