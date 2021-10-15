using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace PreRendering
{
    public static class Decoder
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
        delegate IntPtr ReadImage(string path, int width, int height, out ushort channels, out int bytes_count);
        static ReadImage imread;

        public delegate void ImageDecodedEvent(string path, int[] data, int t);
        public static event ImageDecodedEvent ImageDecoded;

        const string dllPath = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\image-decoder\\x64\\Debug\\image-decoder.dll";
        static IntPtr dllPtr;

        public static void Initialize()
        {
            dllPtr = LoadLibrary(dllPath);

            IntPtr dllAddr;
            dllAddr = GetProcAddress(dllPtr, "initialize");
            initialize = (EmptyCall)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(EmptyCall));
            dllAddr = GetProcAddress(dllPtr, "release");
            release = (EmptyCall)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(EmptyCall));
            dllAddr = GetProcAddress(dllPtr, "imread");
            imread = (ReadImage)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(ReadImage));

            initialize();
        }

        public static void Decode(ref int[] data, string path, int width = 0, int height = 0, int t = -1)
        {
            Debug.Log(string.Format("Decoding\t\t({0})", t));
            IntPtr ptr = imread(path, width, height, out ushort channels, out int bytes_count);

            Marshal.Copy(ptr, data, 0, bytes_count);

            Debug.Log(string.Format("Finished decoding\t({0})", t));
            ImageDecoded.Invoke(path, data, t);
        }

        public static void Deinitialize()
        {
            release();
            FreeLibrary(dllPtr);
        }
    }
}
