using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class DecoderWrapper : MonoBehaviour
{
    [DllImport("kernel32.dll")]
    static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    [DllImport("kernel32.dll")]
    static extern bool FreeLibrary(IntPtr hModule);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr ReadImage(string path, ref int width, ref int height, out int bytes_count);
    ReadImage imread;

    string dllPath = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\testing\\image-decoder\\x64\\Debug\\image-decoder.dll";
    IntPtr dllPtr;

    string image_path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\unity-concept\\Assets\\Rendering\\Testing\\low1.png";

    public Vector2Int resolution;
    public int channels = 3;
    public Texture2D texture;

    void Awake()
    {
        dllPtr = LoadLibrary(dllPath);
        IntPtr dllAddr = GetProcAddress(dllPtr, "imread");
        imread = (ReadImage)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(ReadImage));
    }

    void OnDestroy()
    {
        FreeLibrary(dllPtr);
    }

    void Start()
    {
        int w = resolution.x; int h = resolution.y;

        IntPtr ptr = imread(image_path, ref w, ref h, out int bytes_count);
        byte[] bytes = new byte[bytes_count];
        Marshal.Copy(ptr, bytes, 0, bytes_count);

        Debug.Log(bytes.Length);

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
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(texture, destination);
    }
}
