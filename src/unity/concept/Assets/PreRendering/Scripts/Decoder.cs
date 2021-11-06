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
        static extern IntPtr InitializeBuffer(string samplePath, ref int width, ref int height, int depth);

        [DllImport(dllPath)]
        static extern bool ReadToBuffer(string path, int index);

        [DllImport(dllPath)]
        static extern void ReleaseBuffer();

        public static NativeArray<uint> buffer;

        public delegate void ImageDecodedEvent(string path, long decodingTime, int threadId);
        public static event ImageDecodedEvent ImageDecoded;

        static IntPtr dllPtr, bufferPtr;
        static int imageWidth, imageHeight, bufferSize;

        /// <summary>
        /// Initializes the buffer.
        /// </summary>
        /// <param name="samplePath">The file path of a sample image (for getting the image size).</param>
        /// <param name="depth">Will be set to the total size of the buffer in bytes.</param>
        /// <param name="width">
        /// The desired width to which all textures should be resized.
        /// If the value is -1, it will get set to the actual width of the sample image.
        /// </param>
        /// <param name="height">Same as with the width parameter.</param>
        /// <returns>Returns a pointer to the buffer that images decoded using the 'ReadToBuffer' function will be written to.</returns>
        public static void Initialize(
            string samplePath, int depth, int width = -1, int height = -1)
        {
            bufferPtr = InitializeBuffer(samplePath, ref width, ref height, depth);
            bufferSize = width * height * depth * 2;

            unsafe
            {
                buffer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<uint>(
                    bufferPtr.ToPointer(),
                    bufferSize,
                    Allocator.None);
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref buffer, AtomicSafetyHandle.Create());
#endif

            imageWidth = width;
            imageHeight = height;
        }

        /// <summary>
        /// Decodes an image and writes it into the currently active buffer.
        /// </summary>
        /// <param name="path">The path to the image</param>
        const string a = "";

        /// <summary>
        /// Decodes an image and writes it into the currently active buffer.
        /// </summary>
        /// <param name="path">The path to the image</param>
        /// <param name="index">The buffer position it should be written to.</param>
        /// <param name="threadId">The id will be passed to the ImageDecoded event.</param>
        public static void Decode(string path, int index, int threadId = -1)
        {
            Stopwatch decodingTime = new Stopwatch();

            try
            {
                decodingTime.Start();
                ReadToBuffer(path, index);
                decodingTime.Stop();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return;
            }

            ImageDecoded.Invoke(path, decodingTime.ElapsedMilliseconds, threadId);
        }

        /// <summary>
        /// Releases the currently active buffer.
        /// </summary>
        public static void Deinitialize() => ReleaseBuffer();
    }
}
