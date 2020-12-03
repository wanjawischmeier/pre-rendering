using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CSharpTesting
{
    class Programm
    {
        static string window = "Test Image OMG";
        static string tstpath1 = "E:\\users\\wanja\\Bilder\\Wallpapers\\";
        static string tstpath2 = "C:\\Users\\wanja\\Pictures\\Wallpapers\\";
        static string filepath1 = "E:\\users\\wanja\\Dokumente\\Programmieren\\C#\\pre-rendering\\src\\decoder\\files";
        static string tstimg = tstpath1 + "tst3.jpg";
        static int threads = 4;

        static void Main(string[] args)
        {
            tstimg = "E:\\users\\wanja\\Bilder\\Wallpapers\\tstimg2.jpeg";

            Console.WriteLine("Starting...");

            IntPtr ptr = DLLWrapper.GetUnsignedBytes(tstimg, out int bytes_count);
            byte[] bytes = new byte[bytes_count];
            Marshal.Copy(ptr, bytes, 0, bytes_count);

            Console.WriteLine(bytes.Length);
            Console.WriteLine("Ended");

            File.WriteAllBytes(filepath1 + "tstbinary.bin", bytes);
        }
    }
}
