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
        delegate IntPtr ReadImage(string path, ref int width, ref int height, out int bytes_count);
        static ReadImage imread;

        static string dllPath = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\testing\\image-decoder\\x64\\Debug\\image-decoder.dll";
        static IntPtr dllPtr;

        static string image_path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\unity-concept\\Assets\\Rendering\\Testing\\low1.png";

        static void Main(string[] args)
        {
            dllPtr = LoadLibrary(dllPath);
            IntPtr dllAddr = GetProcAddress(dllPtr, "imread");
            imread = (ReadImage)Marshal.GetDelegateForFunctionPointer(dllAddr, typeof(ReadImage));
            int w = 800; int h = 400;
            IntPtr ptr = imread(image_path, ref w, ref h, out int bytes_count);
            byte[] bytes = new byte[bytes_count];
            Marshal.Copy(ptr, bytes, 0, bytes_count);

            Console.WriteLine(bytes.Length);

            FreeLibrary(dllPtr);
        }
    }
}
