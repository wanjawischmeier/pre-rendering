using System;
using System.Text;
using System.Runtime.InteropServices;

namespace DllTest
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string dllToLoad);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll")]
        static extern bool FreeLibrary(IntPtr hModule);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void EmptyCall();
        static EmptyCall initialize, release;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr ReadImageOld(string path, ref int width, ref int height, out ushort channels, out int bytes_count);
        static ReadImageOld imread_old;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void ReadImage(
            string path, ref int width, ref int height,
            out IntPtr color,
            out int size
        ); static ReadImage imread;

        static string dllPath = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\testing\\image-decoder\\x64\\Debug\\image-decoder.dll";
        static IntPtr dllPtr;

        static string image_path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\unity-concept\\Assets\\Rendering\\Testing\\Sample1\\Main.png";

        static void Main(string[] args)
        {
            dllPtr = LoadLibrary(dllPath);
            IntPtr dllAddr;
            dllAddr = GetProcAddress(dllPtr, "initialize");
            initialize = (EmptyCall)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(EmptyCall));
            dllAddr = GetProcAddress(dllPtr, "release");
            release = (EmptyCall)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(EmptyCall));
            dllAddr = GetProcAddress(dllPtr, "imread_old");
            imread_old = (ReadImageOld)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(ReadImageOld));
            dllAddr = GetProcAddress(dllPtr, "imread");
            imread = (ReadImage)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(ReadImage));
            int w = 800; int h = 400;
            // imread(image_path, ref w, ref h, out IntPtr ptr, out int bytes_count);
            IntPtr ptr;
            int bytes_count;

            ptr = imread_old(image_path, ref w, ref h, out ushort channels, out bytes_count);
            byte[] bytes = new byte[bytes_count];
            Marshal.Copy(ptr, bytes, 0, bytes_count);
            release();
            Console.WriteLine(channels);
            Console.WriteLine(bytes_count);
            FreeLibrary(dllPtr);
        }
    }
}
