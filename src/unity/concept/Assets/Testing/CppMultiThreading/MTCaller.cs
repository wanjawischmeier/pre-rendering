using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class MTCaller : MonoBehaviour
{
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
        string videoPath, FrameEvent callback,
        int width, int height, int depth,
        out VideoInfo info, out IntPtr buffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FrameEvent(long frame);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool EmptyCall();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate VideoInfo TestCall(FrameEvent frameEvent, out VideoInfo videoInfo);

    InitializeBuffer initializeBuffer;
    FrameEvent readToBuffer, frameReady = OnFrameReady;
    EmptyCall releaseBuffer;
    TestCall test;
    IntPtr dllPtr;

    public string dllPath, videoPath;

    private void Start()
    {
        dllPtr = LoadLibrary(dllPath);

        IntPtr dllAddr = GetProcAddress(dllPtr, "Test");
        test = (TestCall)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(TestCall));
        // VideoInfo videoInfo = new VideoInfo();
        VideoInfo info = test(frameReady, out VideoInfo videoInfo);
        Debug.Log(info.width);
        /*
        IntPtr dllAddr = GetProcAddress(dllPtr, "InitializeBuffer");
        initializeBuffer = (InitializeBuffer)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(InitializeBuffer));

        dllAddr = GetProcAddress(dllPtr, "ReadToBuffer");
        readToBuffer = (FrameEvent)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(FrameEvent));

        dllAddr = GetProcAddress(dllPtr, "ReleaseBuffer");
        releaseBuffer = (EmptyCall)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(EmptyCall));

        bool ret = initializeBuffer(videoPath, frameReady, 512, 512, 1, out VideoInfo info, out IntPtr buffer);
        */
    }

    private void OnDestroy()
    {
        FreeLibrary(dllPtr);
    }

    public static void OnFrameReady(long frame)
    {
        Debug.LogFormat("FrameReady callback for frame {0} invoked!", frame);
    }
}
