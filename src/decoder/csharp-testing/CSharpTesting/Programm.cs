using System;
using System.IO;
using System.Text;

namespace CSharpTesting
{
    class Programm
    {
        static string window = "Test Image OMG";
        static string tstpath1 = "C:\\Users\\User\\Pictures\\Wallpaper\\";
        static string tstpath2 = "C:\\Users\\wanja\\Pictures\\Wallpapers\\";
        static string tstimg = tstpath2 + "tstimg.jpg";
        static int threads = 4;

        static void Main(string[] args)
        {
            DLLWrapper.Initialize(ref threads);

            Console.WriteLine("Starting...");
            string raw = DLLWrapper.GetBytes(ref threads, tstimg);
            // char[] chars = raw;
            Console.WriteLine("Ended");

            Console.WriteLine(raw.Length.ToString());
            byte[] raw_bytes = Encoding.ASCII.GetBytes(raw);
            /* ASCII
             * UTF-8
             *    -7
             *    -32
             * Unicode
             * BigEndianUnicode */
            Console.WriteLine(raw_bytes.Length);
            string hex = BitConverter.ToString(raw_bytes);

            File.WriteAllBytes("tstbinary.bin", raw_bytes);
            // Console.WriteLine(hex);

            DLLWrapper.Destroy(ref threads);
        }
    }
}
