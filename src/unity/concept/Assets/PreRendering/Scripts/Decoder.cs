using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace PreRendering
{
    public static class Decoder
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

        /// <summary>
        /// Performs a seek with decoder corresponding to the thread
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool SeekFrameHandler(long frameIdx, int threadIdx);

        /// <summary>
        /// Docodes a frame and copies it to the buffer
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool ReadFrameHandler(int threadIdx);

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

        public static int ImageSize
        {
            get { return info.width * info.height * 3; }
        }

        private static InitializationHandler initializeBuffer;
        private static EmptyCallHandler releaseBuffer;
        private static SeekFrameHandler seekFrame;
        private static ReadFrameHandler readFrame;
        private static IntPtr dllPtr, bufferPtr;
        private static IntPtr[] dataPtr;
        private static Task workerThread;
        private static List<DecodingFrame> pendingFrames;
        private static int instances;
        private static bool working, reading = false;

        private const string relativeDllPath = "src\\video-decoder\\x64\\Debug\\video-decoder.dll";

        #endregion

        /// <summary>
        /// Prepare a given amount of captures and materials for decoding
        /// </summary>
        /// <param name="relativeVideoPath">The video path relative to the repo root directory</param>
        /// <param name="threads">How many instances should be prepared</param>
        /// <param name="dataPointers">Pointers to the data field of each instance</param>
        public static void Initialize(string relativeVideoPath, int threads, out IntPtr[] dataPointers)
        {
            string[] seperator = new string[] { "pre-rendering" };
            string[] split = Application.dataPath.Split(seperator, StringSplitOptions.None);
            string rootPath = split[0].Replace('/', '\\');
            string dllPath = Path.Combine(rootPath, "pre-rendering\\branches\\master\\", relativeDllPath);
            string videoPath = Path.Combine(rootPath, "pre-rendering\\", relativeVideoPath);

            dllPtr = LoadLibrary(dllPath);
            if (dllPtr == IntPtr.Zero)
            {
                Debug.LogError($"Failed to load video decoding library at {dllPath}");
                dataPointers = null;
                return;
            }

            initializeBuffer = LoadFromLibrary<InitializationHandler>("InitializeBuffer");
            releaseBuffer = LoadFromLibrary<EmptyCallHandler>("ReleaseBuffer");
            seekFrame = LoadFromLibrary<SeekFrameHandler>("Seek");
            readFrame = LoadFromLibrary<ReadFrameHandler>("Read");

            pendingFrames = new List<DecodingFrame>();
            instances = threads;

            bufferPtr = initializeBuffer(
                videoPath, instances,
                OnFrameReady, OnError,
                out info);

            dataPtr = new IntPtr[instances];
            Marshal.Copy(bufferPtr, dataPtr, 0, dataPtr.Length);
            dataPointers = dataPtr;

            workerThread = Task.Run(Worker);
        }

        /// <summary>
        /// Stops all currently decoding threads, releases memory allocated by them and frees the plugin
        /// (To be called in MonoBehaviour.OnDestroy)
        /// </summary>
        /// <param name="workerThreadTimeout">
        /// How long to wait for decoding threads to cancel before deallocating memory anyways
        /// (Which will propably result in a crash)
        /// </param>
        public static void Deinitialize(int workerThreadTimeout = 10000)
        {
            if (working)
            {
                working = false;
                workerThread.Wait(workerThreadTimeout);

                if (workerThread.Status == TaskStatus.Running)
                    Debug.LogWarning($"Worker thread not responding (waited {workerThreadTimeout}ms). Deallocating memory anyways, this might result in a crash.");
            }

            FrameReady = delegate { };

            releaseBuffer?.Invoke();
            FreeLibrary(dllPtr);
        }

        /// <summary>
        /// Adds a frame to the decoding queue
        /// </summary>
        /// <param name="frameIdx">The frame to be added</param>
        /// <param name="threadIdx">On which thread it should be decoded</param>
        public static void Decode(long frameIdx, int threadIdx)
        {
            pendingFrames.Add(new DecodingFrame()
            {
                frameIdx = frameIdx,
                threadIdx = threadIdx
            });
            reading = true;
        }

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

            Debug.LogError($"VideoPlayerNativePlugin: {message}\n{openCvInfo} {2}");
        }

        #endregion

        private static void Worker()
        {
            working = true;

            var s = new Stopwatch();

            while (working)
            {
                if (pendingFrames.Count > 0)
                {
                    DecodingFrame frame = pendingFrames[0];
                    pendingFrames.RemoveAt(0);
                    s.Restart();
                    readFrame(frame.threadIdx);
                }
                else if (reading)
                {
                    reading = false;
                    s.Stop();
                    Debug.Log($"Reading frame took {s.ElapsedMilliseconds}ms");
                }
                else Thread.Sleep(100);
            }
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
            Delegate @delegate = Marshal.GetDelegateForFunctionPointer(dllAddr, type);
            return (T)(object)@delegate;
        }
    }
}
