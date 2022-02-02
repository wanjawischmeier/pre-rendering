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

        // TODO: make function pointer delegates private?
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool InitializeBuffer(
            string videoPath, int width, int height, int threads,
            FrameReadyHandler frameCallback, ErrorCallback errorCallback,
            out VideoInfo info, out IntPtr buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool FrameEvent(long frameIdx, int threadIdx, int bufferIdx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ErrorCallback(string message, string error);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void EmptyCall();

        public delegate void FrameReadyHandler(long frameIdx, int threadIdx, int bufferIdx);
        public static event FrameReadyHandler FrameReady;

        private ErrorCallback errorCallback = (message, error) =>
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
        };

        private InitializeBuffer initializeBuffer;
        private FrameEvent readToBuffer;
        private EmptyCall releaseBuffer;
        private IntPtr dllPtr, buffer;
        private Task workerThread;
        private List<PendingFrame> pendingFrames;
        private int instances;
        private bool working, reading = false;

        public VideoInfo info;

        private T LoadFromLibrary<T>(IntPtr library, string name = "")
        {
            Type type = typeof(T);
            IntPtr dllAddr = GetProcAddress(library, name == "" ? type.Name : name);
            Delegate @delegate = Marshal.GetDelegateForFunctionPointer(dllAddr, type);
            return (T)(object)@delegate;
        }

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
                videoPath, 512, 512, instances,
                (long frameIdx, int threadIdx, int bufferIdx) =>
                FrameReady?.Invoke(frameIdx, threadIdx, bufferIdx),
                errorCallback, out info, out buffer);

            workerThread = Task.Run(Worker);
        }

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

        public void Release()
        {
            working = false;
            workerThread.Wait(10000);

            releaseBuffer();
            FreeLibrary(dllPtr);
        }

        private bool ReadToBuffer(PendingFrame frame) => readToBuffer(frame.frameIdx, frame.threadIdx, frame.bufferIdx);

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
    }
}
