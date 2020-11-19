using System;
using System.Runtime.InteropServices;
using System.Text;

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

            Console.WriteLine("Starting...");
            string raw = DLLWrapper.GetBytes(ref threads);
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

            // Console.WriteLine(hex);

            DLLWrapper.Destroy(ref threads);
        }
    }
}
