using System;
using System.Diagnostics;
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
        delegate IntPtr InitializeBuffer(string samplePath, ref int width, ref int height, out int size, out int channels);
        static InitializeBuffer initializeBuffer;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void ReadImageToBuffer(string path);
        static ReadImageToBuffer readImageToBuffer;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void ReleaseBuffer();
        static ReleaseBuffer releaseBuffer;

        public delegate void ImageDecodedEvent(string path, ulong[] data);
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

            bufferPtr = initializeBuffer(samplePath, ref width, ref height, out bufferSize, out channels);

            imageWidth = width;
            imageHeight = height;
            totalSize = bufferSize * channels;
        }

        public static void Decode(string path, int t = -1)
        {
            Console.WriteLine(string.Format("Decoding\t\t({0})", t.ToString()));
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            readImageToBuffer(path);
            stopwatch.Stop();

            short[] temp = new short[totalSize];
            ulong[] data = new ulong[bufferSize];
            Marshal.Copy(bufferPtr, temp, 0, totalSize);

            for (int i = 0; i < bufferSize; i++)
            {
                ushort r = (ushort)temp[i * channels];
                ushort g = (ushort)temp[i * channels + 1];
                ushort b = (ushort)temp[i * channels + 2];
                ushort a = (ushort)temp[i * channels + 3];

                data[i] = Pack(r, g, b, a);
            }

            Console.WriteLine(string.Format("Finished decoding\t({0}) in {1}ms", t.ToString(), stopwatch.ElapsedMilliseconds));
            ImageDecoded.Invoke(path, data);
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

        public static uint Pack2(uint v0, uint v1)
        {
            return v0 << 16 | v1;
        }

        static ulong Pack(uint v0, uint v1)
        {
            return (ulong)v0 << 32 | v1;
        }

        static ulong Pack(ushort v0, ushort v1, ushort v2, ushort v3)
        {
            return Pack(Pack(v0, v1), Pack(v2, v3));
        }

        public static void Unpack(uint v, out ushort v0, out ushort v1)
        {
            v0 = (ushort)(v >> 16);
            v1 = (ushort)(v & 0xFFFF);
        }

        public static void Unpack2(uint v, out uint v0, out uint v1)
        {
            v0 = v >> 16;
            v1 = v & 0xFFFF;
        }

        static void Unpack(ulong v, out uint v0, out uint v1)
        {
            v0 = (uint)(v >> 32);
            v1 = (uint)(v & 0xFFFFFFFF);
        }

        public static void Unpack(ulong v, out ushort v0, out ushort v1, out ushort v2, out ushort v3)
        {
            uint vInt0, vInt1;
            Unpack(v, out vInt0, out vInt1);
            Unpack(vInt0, out v0, out v1);
            Unpack(vInt1, out v2, out v3);
        }
    }
}
