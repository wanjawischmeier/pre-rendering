using System;
using System.Runtime.InteropServices;


namespace CSharpTesting
{
    class Program
    {
        const string decoderdll = "C:\\Users\\User\\Documents\\Programmieren\\Multi-Language\\pre-rendering\\src\\Decoder\\Decoder\\x64\\Debug\\Decoder.dll";

        [DllImport(decoderdll)]
        public static extern void ShowImage(ref string window, ref string path);

        public static string window = "Test Image OMG";
        public static string path = "C:\\Users\\User\\Pictures\\Wallpaper\\tstimg.jpg";

        static void Main(string[] args)
        {
            ShowImage(ref window, ref path);
        }
    }
}
