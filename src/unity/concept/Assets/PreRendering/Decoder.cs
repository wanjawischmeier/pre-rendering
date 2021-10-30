using System;
using System.Runtime.InteropServices;
using Debug = UnityEngine.Debug;

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

        public delegate void ImageDecodedEvent(string path);
        public static event ImageDecodedEvent ImageDecoded;

        const string dllPath = "image-decoder.dll";
        static IntPtr dllPtr, bufferPtr;
        static int imageWidth, imageHeight, bufferSize, totalSize, channels;

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
            totalSize = bufferSize * 4;
        }

        public static void Decode(string path, ref uint[] data, int t = -1)
        {
            Debug.Log(string.Format("Decoding\t\t({0})", t));
            try
            {
                readImageToBuffer(path);

                short[] temp = new short[totalSize];
                Marshal.Copy(bufferPtr, temp, 0, totalSize);

                for (int i = 0; i < data.Length; i++)
                {
                    ushort a = (ushort)temp[i * 2];
                    ushort b = (ushort)temp[i * 2 + 1];

                    data[i] = Pack(a, b);
                }

                ImageDecoded.Invoke(path);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            Debug.Log(string.Format("Finished decoding\t({0})", t));
        }

        public static void Deinitialize()
        {
            releaseBuffer();
            FreeLibrary(dllPtr);
        }

        public static uint Pack(ushort v0, ushort v1)
        {
            return (uint)v0 << 16 | v1;
        }

        public static void Unpack(uint v, out ushort v0, out ushort v1)
        {
            v0 = (ushort)(v >> 16);
            v1 = (ushort)(v & 0xFFFF);
        }
    }
}
