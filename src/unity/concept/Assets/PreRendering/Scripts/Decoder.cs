using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace PreRendering
{
    public static class Decoder
    {
        const string DllPath = "image-decoder.dll";

        [DllImport(DllPath)]
        static extern IntPtr InitializeBuffer(string samplePath, ref int width, ref int height, int depth);

        [DllImport(DllPath)]
        static extern bool ReadToBuffer(string path, int index);

        [DllImport(DllPath)]
        static extern bool ReleaseBuffer();

        public delegate void ImageDecodedEvent(string path, int index, int threadId, long decodingTime);
        public static event ImageDecodedEvent ImageDecoded;

        public static IntPtr bufferPointer;
        static int imageWidth, imageHeight;

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
            bufferPointer = InitializeBuffer(samplePath, ref width, ref height, depth);

            imageWidth = width;
            imageHeight = height;
        }

        /// <summary>
        /// Decodes an image and writes it into the currently active buffer.
        /// </summary>
        /// <param name="path">The path to the image</param>
        /// <param name="index">The buffer position it should be written to.</param>
        /// <param name="threadId">The id will be passed to the ImageDecoded event.</param>
        public static bool Decode(string path, int index, int threadId = 0)
        {
            var decodingTime = new Stopwatch();
            bool result;

            try
            {
                decodingTime.Start();
                result = ReadToBuffer(path, index);
                decodingTime.Stop();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                result = false;
            }

            ImageDecoded.Invoke(path, index, threadId, decodingTime.ElapsedMilliseconds);
            return result;
        }

        /// <summary>
        /// Releases the currently active buffer.
        /// </summary>
        public static void Deinitialize()
        {
            if (!ReleaseBuffer())
                Debug.LogError(
                    "Failed to release raw texture buffer with pointer " +
                    $"<{(bufferPointer.ToInt32() == 0 ? "nullptr" : bufferPointer.ToString())}>");
        }
    }
}
