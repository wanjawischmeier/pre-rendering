using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PreRendering
{
    public partial class Decoder
    {
        #region Native Plugin

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr hModule);

        /// <summary>
        /// Initializes buffer and video capture
        /// </summary>
        /// <param name="threads">Number of capture threads to be initialized</param>
        /// <param name="frameCallback">The function to be called after a frame has been grabbed</param>
        /// <param name="errorCallback">The function to be called if an error occurs</param>
        /// <param name="videoInfo">Containing basic information about the video</param>
        /// <returns>The pointer of the buffer grabbed frames will be written to</returns>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr InitializationHandler(
            string videoPath, int threads,
            FrameReadyHandler frameCallback, ErrorCallback errorCallback,
            out VideoInfo videoInfo);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool FrameHandler(long frameIdx, int threadIdx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate long CurrentFrameHandler(int threadIdx);

        /// <param name="message">A custom message by the plugin</param>
        /// <param name="error">OpenCV or std error message, plugin error if left empty</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ErrorCallback(string message, string error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void EmptyCallHandler();

        #endregion

        #region Data

        [Serializable]
        public struct VideoInfo
        {
            public int width, height, fps;
            public long frame_count;
        };

        private struct DecodingFrame
        {
            public long frameIdx;
            public int threadIdx;
        }

        public delegate void FrameReadyHandler(long frameIdx, int threadIdx);
        public static event FrameReadyHandler FrameReady;

        public static VideoInfo info;
        public static bool invokeFrameReadyEvents = false;
        public static Decoder[] decoders;

        private static InitializationHandler initializeDecoder;
        private static EmptyCallHandler releaseDecoder;
        private static FrameHandler seekFrame, readFrame;
        private static CurrentFrameHandler currentFrame;
        private static IntPtr dllPtr, bufferPtr;
        private static IntPtr[] dataPtr;
        private static List<DecodingFrame> pendingFrames;
        private static bool reading = false;

        private const string relativeDllPath = "branches\\master\\src\\video-decoder\\x64\\Debug\\video-decoder.dll";

        /// <summary>
        /// How long to wait for decoding threads to cancel before deallocating memory anyways
        /// (Which will propably result in a crash)
        /// </summary>
        private const int workerThreadTimeout = 10000;

        public static int ImageSize => info.width * info.height * 3;

        #endregion

        #region Events

        private static void OnFrameReady(long frameIdx, int threadIdx)
        {
            if (invokeFrameReadyEvents)
                FrameReady?.Invoke(frameIdx, threadIdx);
        }

        /// <summary>
        /// Formats error messages by the plugin to be displayed in the unity console
        /// </summary>
        private static void OnError(string message, string error)
        {
            // Custom error, not thrown by opencv
            if (error == "")
            {
                Debug.LogError($"VideoPlayerNativePlugin: {message}");
                return;
            }

            int pathStart = error.IndexOf(')');
            int pathEnd = error.IndexOf(".cpp", pathStart);
            int fileStart = error.LastIndexOf('\\', pathEnd);

            string openCvInfo = error.Substring(0, pathStart + 1);
            string errorMessage = error.Substring(fileStart + 1)
                .Replace(": error:", "\nerror:")
                .Replace(") ", ")\n");

            Debug.LogError($"VideoPlayerNativePlugin: {message}\n{openCvInfo}\n{errorMessage}");
        }

        #endregion

        /// <summary>
        /// Prepare a given amount of captures and materials for decoding
        /// </summary>
        /// <param name="relativeVideoPath">The video path relative to the repo root directory</param>
        /// <param name="threads">How many instances should be prepared</param>
        /// <param name="dataPointers">Pointers to the data field of each instance</param>
        public static Decoder[] Initialize(string relativeVideoPath, int threads, out IntPtr[] dataPointers)
        {
            string[] seperator = new string[] { "pre-rendering" };
            string[] split = Application.dataPath.Split(seperator, StringSplitOptions.None);
            string rootPath = split[0].Replace('/', '\\');
            string dllPath = Path.Combine(rootPath, "pre-rendering\\", relativeDllPath);
            string videoPath = Path.Combine(rootPath, "pre-rendering\\renders\\", relativeVideoPath);

            dllPtr = LoadLibrary(dllPath);
            if (dllPtr == IntPtr.Zero)
            {
                Debug.LogError($"Failed to load video decoding library at {dllPath}");
                dataPointers = null;
                return null;
            }

            initializeDecoder = LoadFromLibrary<InitializationHandler>("InitializeDecoder");
            releaseDecoder = LoadFromLibrary<EmptyCallHandler>("ReleaseDecoder");
            seekFrame = LoadFromLibrary<FrameHandler>("Seek");
            readFrame = LoadFromLibrary<FrameHandler>("Read");
            currentFrame = LoadFromLibrary<CurrentFrameHandler>("CurrentFrame");

            pendingFrames = new List<DecodingFrame>();

            bufferPtr = initializeDecoder(
                videoPath, threads,
                OnFrameReady, OnError,
                out info);

            dataPtr = new IntPtr[threads];
            Marshal.Copy(bufferPtr, dataPtr, 0, dataPtr.Length);
            dataPointers = dataPtr;

            decoders = new Decoder[threads];
            for (int i = 0; i < threads; i++)
                decoders[i] = new Decoder(i);

            return decoders;
        }

        /// <summary>
        /// Stops all currently decoding threads, releases memory allocated by them and frees the plugin
        /// (To be called in MonoBehaviour.OnDestroy)
        /// </summary>
        public static void Deinitialize()
        {
            FrameReady = delegate { };
            bool success = true;

            foreach (var decoder in decoders)
                if (!decoder.Wait())
                    success = false;

            if (!success)
                Debug.LogWarning($"Some threads are not responding. Deallocating memory anyways, this might result in a crash.");

            releaseDecoder?.Invoke();
            FreeLibrary(dllPtr);
        }

        /// <summary>
        /// Loads an instance of the given delegate from the library
        /// </summary>
        /// <typeparam name="T">The delegate of the desired function</typeparam>
        /// <param name="library">The pointer to an already loaded library</param>
        /// <param name="name">The name of the function, using delegate name by default</param>
        private static T LoadFromLibrary<T>(string name)
        {
            Type type = typeof(T);
            IntPtr dllAddr = GetProcAddress(dllPtr, name);
            if (dllAddr == IntPtr.Zero)
            {
                Debug.LogError($"Failed to get process address for {name}");
                return default(T);
            }
            Delegate @delegate = Marshal.GetDelegateForFunctionPointer(dllAddr, type);
            return (T)(object)@delegate;
        }
    }
}