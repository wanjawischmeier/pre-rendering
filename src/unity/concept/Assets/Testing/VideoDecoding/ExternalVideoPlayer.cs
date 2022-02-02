using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace PreRendering
{
    public class ExternalVideoPlayer
    {
        [Serializable]
        public struct VideoInfo
        {
            public int width, height, fps;
            public long frame_count;
        };

        private struct PendingFrame
        {
            public long frameIdx;
            public int threadIdx, bufferIdx;
        }

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
        /// <param name="buffer">The pointer of the buffer grabbed frames will be written to</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool InitializeBuffer(
            string videoPath, int threads,
            FrameReadyHandler frameCallback, ErrorCallback errorCallback,
            out VideoInfo videoInfo, out IntPtr buffer);

        /// <summary>
        /// Docodes a frame and copies it to the buffer
        /// </summary>
        /// <param name="frameIdx">The target frame</param>
        /// <param name="threadIdx">On which thread it will be decoded</param>
        /// <param name="bufferIdx">The target position in the buffer</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool FrameEvent(long frameIdx, int threadIdx, int bufferIdx);

        /// <param name="message">A custom message by the plugin</param>
        /// <param name="error">OpenCV or std error message, plugin error if left empty</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ErrorCallback(string message, string error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void EmptyCall();

        public delegate void FrameReadyHandler(long frameIdx, int threadIdx, int bufferIdx);
        public static event FrameReadyHandler FrameReady;

        public VideoInfo info;
        public RawTexture.NativeBuffer computeBuffer;

        private InitializeBuffer initializeBuffer;
        private FrameEvent readToBuffer;
        private EmptyCall releaseBuffer;
        private IntPtr dllPtr, bufferPtr;
        private Task workerThread;
        private List<PendingFrame> pendingFrames;
        private int instances;
        private bool working, reading = false;

        public ExternalVideoPlayer(string videoPath, int threads)
        {
            string dllPath = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\branches\\master\\src\\video-decoder\\x64\\Debug\\video-decoder.dll";
            dllPtr = LoadLibrary(dllPath);

            initializeBuffer = LoadFromLibrary<InitializeBuffer>(dllPtr);
            readToBuffer = LoadFromLibrary<FrameEvent>(dllPtr, "ReadToBuffer");
            releaseBuffer = LoadFromLibrary<EmptyCall>(dllPtr, "ReleaseBuffer");
            
            pendingFrames = new List<PendingFrame>();
            instances = threads;

            bool ret = initializeBuffer(
                videoPath, instances,
                OnFrameReady, OnError,
                out info, out bufferPtr);

            computeBuffer = new RawTexture.NativeBuffer(bufferPtr, info.width, info.height, instances, RawTexture.Format.RGBA32);

            workerThread = Task.Run(Worker);
        }

        private bool ReadToBuffer(PendingFrame frame) => readToBuffer(frame.frameIdx, frame.threadIdx, frame.bufferIdx);

        private void OnFrameReady(long frameIdx, int threadIdx, int bufferIdx) =>
                FrameReady?.Invoke(frameIdx, threadIdx, bufferIdx);

        /// <summary>
        /// Formats error messages by the plugin to be displayed in the unity console
        /// </summary>
        private void OnError(string message, string error)
        {
            // Custom error, not thrown by opencv
            if (error == "")
            {
                Debug.LogErrorFormat("VideoPlayerNativePlugin: {0}", message);
                return;
            }

            int pathStart = error.IndexOf(')');
            int pathEnd = error.IndexOf(".cpp", pathStart);
            int fileStart = error.LastIndexOf('\\', pathEnd);

            string openCvInfo = error.Substring(0, pathStart + 1);
            string errorMessage = error.Substring(fileStart + 1)
                .Replace(": error:", "\nerror:")
                .Replace(") ", ")\n");

            Debug.LogErrorFormat(
                "VideoPlayerNativePlugin: {0}\n{1} {2}",
                message, openCvInfo, errorMessage);
        }

        /// <summary>
        /// Loads the instance of the given delegate from the library
        /// </summary>
        /// <typeparam name="T">The delegate of the desired function</typeparam>
        /// <param name="library">The pointer to an already loaded library</param>
        /// <param name="name">The name of the function, using delegate name by default</param>
        private T LoadFromLibrary<T>(IntPtr library, string name = "")
        {
            Type type = typeof(T);
            IntPtr dllAddr = GetProcAddress(library, name == "" ? type.Name : name);
            Delegate @delegate = Marshal.GetDelegateForFunctionPointer(dllAddr, type);
            return (T)(object)@delegate;
        }

        private void Worker()
        {
            working = true;

            var s = Stopwatch.StartNew();

            while (working)
            {
                if (pendingFrames.Count > 0)
                {
                    PendingFrame frame = pendingFrames[0];
                    pendingFrames.RemoveAt(0);
                    ReadToBuffer(frame);
                }
                else if (reading)
                {
                    reading = false;
                    s.Stop();
                    Debug.Log(s.ElapsedMilliseconds);
                }
                else Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Adds a frame to the decoding queue
        /// </summary>
        /// <param name="frameIdx">The frame to be added</param>
        /// <param name="threadIdx">On which thread it should be decoded</param>
        /// <param name="bufferIdx">The buffer index to which it should be copied</param>
        public void ReadToBuffer(long frameIdx, int threadIdx, int bufferIdx)
        {
            pendingFrames.Add(new PendingFrame()
            {
                frameIdx = frameIdx,
                threadIdx = threadIdx,
                bufferIdx = bufferIdx
            });
            reading = true;
        }

        /// <summary>
        /// Stops all currently decoding threads, releases memory allocated by them and frees the plugin
        /// (To be called in MonoBehaviour.OnDestroy)
        /// </summary>
        /// <param name="workerThreadTimeout">
        /// How long to wait for the worker thread to cancel before deallocating memory anyways
        /// (Which will propably result in a crash)
        /// </param>
        public void Release(int workerThreadTimeout = 10000)
        {
            working = false;
            workerThread.Wait(workerThreadTimeout);

            if (workerThread.Status == TaskStatus.Running)
                Debug.LogWarningFormat(
                    "Worker thread not responding (waited {0}ms). Deallocating memory anyways, this might result in a crash.",
                    workerThreadTimeout);

            releaseBuffer();
            computeBuffer.Release();
            FreeLibrary(dllPtr);
        }
    }
}
