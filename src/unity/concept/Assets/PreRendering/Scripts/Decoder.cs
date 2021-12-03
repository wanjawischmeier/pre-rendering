using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PreRendering
{
    public static class Decoder
    {
        private const string DllPath = "image-decoder.dll";

        [DllImport(DllPath)]
        private static extern IntPtr InitializeBuffer(string samplePath, ref int width, ref int height, int depth);

        [DllImport(DllPath)]
        private static extern bool ReadToBuffer(string path, int index);

        [DllImport(DllPath)]
        private static extern bool ReleaseBuffer();

        public static IntPtr bufferPointer;
        private static int imageWidth, imageHeight;

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
        public static bool Decode(string path, int index, out long elapsedMilliseconds)
        {
            var decodingTime = new Stopwatch();
            bool result;

            try
            {
                // Debug.Log($"Trying to read {path} to index {index}");
                decodingTime.Start();
                if (index >= 0)
                    result = ReadToBuffer(path, index);
                else
                {
                    Debug.Log($"Aborting image {path} due to invalid index");
                    result = false;
                }
                decodingTime.Stop();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                result = false;
            }

            elapsedMilliseconds = decodingTime.ElapsedMilliseconds;
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
