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
        const string dllPath = "image-decoder.dll";

        [DllImport(dllPath)]
        static extern IntPtr InitializeBuffer(string samplePath, ref int width, ref int height, out int size);

        [DllImport(dllPath)]
        static extern bool ReadToBuffer(string path);

        [DllImport(dllPath)]
        static extern void ReleaseBuffer();

        public static NativeArray<uint> buffer;

        public delegate void ImageDecodedEvent(string path, DecodingStats stats);
        public static event ImageDecodedEvent ImageDecoded;

        static IntPtr dllPtr, bufferPtr;
        static int imageWidth, imageHeight, bufferSize, totalSize, channels;

        public struct DecodingStats
        {
            public int ThreadId;
            public long Decoding, Copying, Packing;

            public override string ToString()
            {
                return (
                    $"ThreadID:\t{ThreadId}\n" +
                    $"Decoding:\t{Decoding}ms\n" +
                    $"Copying:\t{Copying}ms\n" +
                    $"Packing:\t{Packing}ms\n" +
                    $"Total:\t{Decoding + Copying + Packing}ms");
            }
        }

        public static void Initialize(
            string samplePath, int width = -1, int height = -1)
        {
            bufferPtr = InitializeBuffer(samplePath, ref width, ref height, out bufferSize);

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
#error The 'ENABLE_UNITY_COLLECTIONS_CHECKS' macro is not defined
#endif

            ReadToBuffer(samplePath);

            imageWidth = width;
            imageHeight = height;
            totalSize = bufferSize * 4;
        }

        public static void Decode(string path, ref uint[] data, CancellationToken token, int t = -1)
        {
            Debug.Log($"Decoding\t\t({t})");
            try
            {
                Stopwatch timeDecoding = new Stopwatch();
                timeDecoding.Start();
                ReadToBuffer(path);
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

            Debug.Log($"Finished decoding\t({t})");
        }

        public static void Deinitialize() => ReleaseBuffer();

        public static uint Pack(ushort v0, ushort v1)
        {
            return (uint)v0 << 16 | v1;
        }

        public static void Unpack(uint v, out ushort v0, out ushort v1)
        {
            v0 = (ushort)(v & 0xFFFF);
            v1 = (ushort)(v >> 16);
        }
    }
}
