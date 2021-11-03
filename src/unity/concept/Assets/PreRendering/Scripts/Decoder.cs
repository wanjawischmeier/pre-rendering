using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        delegate bool ReadImageToBuffer(string path);
        static ReadImageToBuffer readImageToBuffer;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void ReleaseBuffer();
        static ReleaseBuffer releaseBuffer;

        public static NativeArray<uint> buffer;

        public delegate void ImageDecodedEvent(string path, DecodingStats stats);
        public static event ImageDecodedEvent ImageDecoded;

        const string dllPath = "image-decoder.dll";
        static IntPtr dllPtr, bufferPtr;
        static int imageWidth, imageHeight, bufferSize, totalSize, channels;

        public struct DecodingStats
        {
            public int ThreadId;
            public long Decoding, Copying, Packing;

            public override string ToString()
            {
                return string.Format(
                    "ThreadID:\t{0}\n" +
                    "Decoding:\t{1}ms\n" +
                    "Copying:\t{2}ms\n" +
                    "Packing:\t{3}ms\n" +
                    "Total:\t{4}ms",
                    ThreadId, Decoding, Copying, Packing,
                    Decoding + Copying + Packing);
            }
        }

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

            unsafe
            {
                buffer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<uint>(
                    bufferPtr.ToPointer(),
                    bufferSize * 2,
                    Allocator.None);
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref buffer, AtomicSafetyHandle.Create());
#else
#error The 'ENABLE_UNITY_COLLECTIONS_CHECKS' symbol needs to be set. Enable it under 'Project Settings/Player/Other Settings/Scripting Define Symbols'.
#endif
            readImageToBuffer(samplePath);

            imageWidth = width;
            imageHeight = height;
            totalSize = bufferSize * 4;
        }

        public static void Decode(string path, ref uint[] data, CancellationToken token, int t = -1)
        {
            Debug.Log(string.Format("Decoding\t\t({0})", t));
            try
            {
                Stopwatch timeDecoding = new Stopwatch();
                timeDecoding.Start();
                readImageToBuffer(path);
                timeDecoding.Stop();
                Debug.Log(buffer[0]);
                Stopwatch timeCopying = new Stopwatch();
                timeCopying.Start();
                short[] temp = new short[totalSize];
                Marshal.Copy(bufferPtr, temp, 0, totalSize);
                timeCopying.Stop();

                Stopwatch timePacking = new Stopwatch();
                timePacking.Start();
                for (int i = 0; i < bufferSize * 2; i++)
                {
                    ushort a = (ushort)temp[i * 2];
                    ushort b = (ushort)temp[i * 2 + 1];

                    data[i] = Pack(a, b);

                    if (token.IsCancellationRequested)
                    {
                        Debug.Log("Cancellation requested");
                        token.ThrowIfCancellationRequested();
                        return;
                    }
                }
                timePacking.Stop();

                DecodingStats stats = new DecodingStats()
                {
                    ThreadId = t,
                    Decoding = timeDecoding.ElapsedMilliseconds,
                    Copying = timeCopying.ElapsedMilliseconds,
                    Packing = timePacking.ElapsedMilliseconds
                };
                ImageDecoded.Invoke(path, stats);
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
