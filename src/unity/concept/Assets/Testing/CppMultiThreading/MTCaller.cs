using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public class MTCaller : MonoBehaviour
{
    [Serializable]
    public struct VideoInfo
    {
        public int width, height, fps;
        public long frame_count;
    };

    [DllImport("kernel32.dll")]
    static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    [DllImport("kernel32.dll")]
    static extern bool FreeLibrary(IntPtr hModule);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool InitializeBuffer(
        string videoPath, int width, int height, int threads,
        FrameEvent frameCallback, ErrorCallback errorCallback,
        out VideoInfo info, out int error, out IntPtr buffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FrameEvent(long frameIdx, int threadIdx, int bufferIdx);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void ErrorCallback(string message, string error);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void EmptyCall();

    ErrorCallback errorCallback = (message, error) =>
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
        
        string openCvInfo = error.Substring(0, pathStart +1);
        string errorMessage = error.Substring(fileStart + 1)
            .Replace(": error:", "\nerror:")
            .Replace(") ", ")\n");

        Debug.LogErrorFormat(
            "VideoPlayerNativePlugin: {0}\n{1} {2}",
            message, openCvInfo, errorMessage);
    };

    InitializeBuffer initializeBuffer;
    FrameEvent readToBuffer;
    EmptyCall releaseBuffer;
    IntPtr dllPtr, buffer;

    public string dllPath, videoPath;
    public int error;
    public VideoInfo videoInfo;

    private T LoadFromLibrary<T>(IntPtr library, string name = "")
    {
        Type type = typeof(T);
        IntPtr dllAddr = GetProcAddress(library, name == "" ? type.Name : name);
        Delegate @delegate = Marshal.GetDelegateForFunctionPointer(dllAddr, type);
        return (T)(object)@delegate;
    }

    private void Start()
    {
        dllPtr = LoadLibrary(dllPath);
        initializeBuffer = LoadFromLibrary<InitializeBuffer>(dllPtr);
        readToBuffer = LoadFromLibrary<FrameEvent>(dllPtr, "ReadToBuffer");
        releaseBuffer = LoadFromLibrary<EmptyCall>(dllPtr, "ReleaseBuffer");

        bool ret = initializeBuffer(
            videoPath, 512, 512, 1,
            OnFrameReady, errorCallback,
            out videoInfo, out error, out buffer);

        StartCoroutine(TestSeeks());
    }

    private void OnDestroy()
    {
        releaseBuffer();
        FreeLibrary(dllPtr);
    }

    private IEnumerator TestSeeks()
    {
        yield return new WaitForSeconds(2);

        Task.Run(() =>
        {
            readToBuffer(UnityEngine.Random.Range(0, (int)videoInfo.frame_count - 1), 0, 0);
        });
    }

    private void OnFrameReady(long frameIdx, int threadIdx, int bufferIdx)
    {
        Debug.LogFormat(
            "FrameReady callback for frame {0} from thread {1} invoked (stored at {2})",
            frameIdx, threadIdx, bufferIdx);

        // readToBuffer(UnityEngine.Random.Range(0, (int)videoInfo.frame_count - 1), 0, 0);
    }
}
