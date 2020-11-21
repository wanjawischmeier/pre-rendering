using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class ToTexture : MonoBehaviour
{
    static string window = "Test Image OMG";
    static string tstpath1 = "C:\\Users\\User\\Pictures\\Wallpaper\\";
    static string tstpath2 = "C:\\Users\\wanja\\Pictures\\Wallpapers\\";
    static string tstimg = tstpath2 + "tstimg.jpg";

    static string tstbin = "C:\\Users\\wanja\\Documents\\dev\\csharp\\pre-rendering\\src\\decoder\\csharp-testing\\CSharpTesting\\bin\\x64\\Debug\\netcoreapp3.1\\tstbinary.bin";

    const string projectpath1 = "C:\\Users\\User\\Documents\\Programmieren\\Multi-Language\\pre-rendering";
    const string projectpath2 = "C:\\Users\\wanja\\Documents\\dev\\csharp\\pre-rendering\\";
    const string decoderdll = projectpath2 + "src\\decoder\\cpp-decoder-class\\x64\\Debug\\Decoder.dll";

    static int threads = 4;

    public Texture2D read;

    public static class DLLWrapper
    {
        [DllImport(decoderdll)]
        public static extern void Initialize(ref int threads);
        [DllImport(decoderdll)]
        public static extern void Create(ref string mapFile);
        [DllImport(decoderdll)]
        public static extern void SetFrame(ref double index);
        [DllImport(decoderdll)]
        public static extern void ShowCustomImage(ref string window, ref string path);
        [DllImport(decoderdll)]
        public static extern void ShowImage(ref int id, ref string window);
        [DllImport(decoderdll)]
        public static extern string GetImage(ref int id);
        [DllImport(decoderdll, EntryPoint = "GetBytes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern string GetBytes(ref int id, string path);

        [DllImport(decoderdll, EntryPoint = "testString2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string testString2();

        [DllImport(decoderdll)]
        public static extern void Destroy(ref int id);
    }

    void DLLTest()
    {
        DLLWrapper.Initialize(ref threads);

        Debug.Log("Starting...");
        string raw = DLLWrapper.GetBytes(ref threads, tstimg);
        // char[] chars = raw;
        Debug.Log("Ended");

        Debug.Log(raw.Length.ToString());
        byte[] raw_bytes = Encoding.ASCII.GetBytes(raw);
        /* ASCII
         * UTF-8
         *    -7
         *    -32
         * Unicode
         * BigEndianUnicode */
        Debug.Log(raw_bytes.Length);
        string hex = BitConverter.ToString(raw_bytes);

        // Debug.Log(hex);

        DLLWrapper.Destroy(ref threads);
    }

    void Start()
    {
        byte[] binary = File.ReadAllBytes(tstbin);
        Debug.Log(binary.Length);

        read = new Texture2D(2, 2);
        read.LoadImage(binary);
    }

    void Update()
    {
        
    }
}
