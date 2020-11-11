using System;


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

            DLLWrapper.GetImage(ref threads);

            DLLWrapper.Destroy(ref threads);
        }
    }
}
