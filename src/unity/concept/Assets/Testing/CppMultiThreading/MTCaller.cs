using System;
using System.Collections;
using System.Runtime.InteropServices;
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
        string videoPath, FrameEvent callback,
        int width, int height, int depth,
        out VideoInfo info, out IntPtr buffer);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void FrameEvent(long frame);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void EmptyCall();




    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void TestCallback(FrameEvent frameEvent);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool TestInit(string videoPath, int threads, out VideoInfo videoInfo, out int error);

    InitializeBuffer initializeBuffer;
    FrameEvent readToBuffer, frameReady = OnFrameReady;
    EmptyCall releaseBuffer;
    IntPtr dllPtr;

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

        TestCallback testCallback = LoadFromLibrary<TestCallback>(dllPtr);
        TestInit testInit = LoadFromLibrary<TestInit>(dllPtr);
        releaseBuffer = LoadFromLibrary<EmptyCall>(dllPtr, "ReleaseBuffer");


        bool res = testInit(videoPath, 2, out videoInfo, out error);
        Debug.Log(res);

        // testCallback(frameReady);

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
        releaseBuffer();
        FreeLibrary(dllPtr);
    }

    public static void OnFrameReady(long frame)
    {
        Debug.LogFormat("FrameReady callback for frame {0} invoked!", frame);
    }
}
