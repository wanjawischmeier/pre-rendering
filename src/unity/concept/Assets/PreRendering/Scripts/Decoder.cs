using System;
using System.Runtime.InteropServices;
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

        public delegate void ImageDecodedEvent(string path, long decodingTime, int threadId);
        public static event ImageDecodedEvent ImageDecoded;

        static IntPtr dllPtr, bufferPtr;
        static int imageWidth, imageHeight, bufferSize, totalSize, channels;

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
#endif

            imageWidth = width;
            imageHeight = height;
            totalSize = bufferSize * 4;
        }

        public static void Decode(string path, int threadId = -1)
        {
            Stopwatch decodingTime = new Stopwatch();

            try
            {
                decodingTime.Start();
                ReadToBuffer(path);
                decodingTime.Stop();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }

            ImageDecoded.Invoke(path, decodingTime.ElapsedMilliseconds, threadId);
        }

        public static void Deinitialize() => ReleaseBuffer();
    }
}
