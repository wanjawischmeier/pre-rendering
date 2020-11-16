using System;
using System.Runtime.InteropServices;

namespace CSharpTesting
{
    class Programm
    {
        static string window = "Test Image OMG";
        static string path = "C:\\Users\\User\\Pictures\\Wallpaper\\tstimg.jpg";
        static int threads = 4;

        static void Main(string[] args)
        {
            DLLWrapper.Initialize(ref threads);

            // DLLWrapper.ShowCustomImage(ref window, ref path);
            IntPtr ptr = new IntPtr();
            DLLWrapper.GetImage(ref ptr, out int size, ref threads);
            byte[] target = new byte[size];
            //IntPtr read = Marshal.ReadIntPtr(data, 20000);
            byte readb = Marshal.ReadByte(ptr);
            //byte[] ptr = *data.ToPointer();
            Marshal.Copy(ptr, target, 0, size);
            
            DLLWrapper.Destroy(ref threads);
        }
    }
}
