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
    delegate void EmptyCall();
    static EmptyCall initialize, release;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate IntPtr ReadImageOld(string path, ref int width, ref int height, out int channels, out int bytes_count);
    static ReadImageOld imread_old;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    delegate void ReadImage(
        string path, ref int width, ref int height,
        IntPtr color,
        out int size
    );
    ReadImage imread;

    string dllPath = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\testing\\image-decoder\\x64\\Debug\\image-decoder.dll";
    IntPtr dllPtr;

    string image_path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\unity-concept\\Assets\\Rendering\\Testing\\Sample1\\Main.png";

    public Vector2Int resolution;
    int bytes_count;
    IntPtr ptr;
    [Range(2, 10)]
    public int slice = 2;
    public Texture2D texture;

    void Awake()
    {
        dllPtr = LoadLibrary(dllPath);
        IntPtr dllAddr;
        dllAddr = GetProcAddress(dllPtr, "initialize");
        initialize = (EmptyCall)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(EmptyCall));
        dllAddr = GetProcAddress(dllPtr, "release");
        release = (EmptyCall)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(EmptyCall));
        dllAddr = GetProcAddress(dllPtr, "imread_old");
        imread_old = (ReadImageOld)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(ReadImageOld));
        dllAddr = GetProcAddress(dllPtr, "imread");
        imread = (ReadImage)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(ReadImage));
    }

    void OnDestroy()
    {
        release();
        FreeLibrary(dllPtr);
    }

    void Start()
    {
        int w = resolution.x; int h = resolution.y;

        ptr = imread_old(image_path, ref w, ref h, out int channels, out bytes_count);
        Debug.Log(channels);
        Debug.Log(bytes_count);

        short[] bytes = new short[bytes_count];
        Marshal.Copy(ptr, bytes, 0, bytes_count);

        texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGBA64, false);

        Color[] colorArray = new Color[bytes.Length / channels];

        for (int i, x = 0; x < resolution.x; x++)
        {
            for (int y = 0; y < resolution.y; y++)
            {
                i = (x * channels) + (y * channels) * resolution.x;

                var color = new Color(
                    bytes[i + 0], bytes[i + 1],
                    bytes[i + 2], bytes[i + 3]
                );

                colorArray[i / channels] = color;
            }
        }

        texture.SetPixels(colorArray);
        texture.Apply();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(texture, destination);
    }
}
