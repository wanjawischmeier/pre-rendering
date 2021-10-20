using System;
using System.Runtime.InteropServices;

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
        delegate IntPtr InitializeBuffer(string samplePath, ref int width, ref int height, out int size);
        static InitializeBuffer initializeBuffer;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void ReadImageToBuffer(string path);
        static ReadImageToBuffer readImageToBuffer;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void ReleaseBuffer();
        static ReleaseBuffer releaseBuffer;

        public delegate void ImageDecodedEvent(string path, uint[] data);
        public static event ImageDecodedEvent ImageDecoded;

        const string dllPath = "image-decoder.dll";
        static IntPtr dllPtr, bufferPtr;
        static int imageWidth, imageHeight, bufferSize;

        public static void Initialize(
            string samplePath, int width = -1, int height = -1)
        {
            dllPtr = LoadLibrary(dllPath);

            IntPtr dllAddr;
            dllAddr = GetProcAddress(dllPtr, "InitializeBuffer");
            initializeBuffer = (InitializeBuffer)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(InitializeBuffer));
            dllAddr = GetProcAddress(dllPtr, "ReadToBuffer");
            readImageToBuffer = (ReadImageToBuffer)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(ReadImageToBuffer));
            dllAddr = GetProcAddress(dllPtr, "ReleaseBuffer");
            releaseBuffer = (ReleaseBuffer)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(ReleaseBuffer));

            bufferPtr = initializeBuffer(samplePath, ref width, ref height, out bufferSize);

            imageWidth = width;
            imageHeight = height;
        }

        public static void Decode(string path, int t = -1)
        {
            Console.WriteLine(string.Format("Decoding\t\t({0})", t.ToString()));
            readImageToBuffer(path);
            
            short[] temp = new short[bufferSize];
            Marshal.Copy(bufferPtr, temp, 0, bufferSize);
            uint[] data = Array.ConvertAll(temp, val => ((uint)val));
            
            Console.WriteLine(string.Format("Finished decoding\t({0})", t.ToString()));
            ImageDecoded.Invoke(path, data);
        }

        public static void Deinitialize()
        {
            releaseBuffer();
            FreeLibrary(dllPtr);
        }
    }
}
