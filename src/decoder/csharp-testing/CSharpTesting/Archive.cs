using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CSharpTesting
{
    class Archive
    {
        public static class DLLWrapper
        {
            const string projectpath1 = "E:\\users\\wanja\\Dokumente\\Programmieren\\C#\\pre-rendering\\";
            const string projectpath2 = "C:\\Users\\wanja\\Documents\\dev\\csharp\\pre-rendering\\";
            const string decoderdll = projectpath1 + "src\\decoder\\cpp-decoder-class\\x64\\Debug\\Decoder.dll";
            // C:\Users\wanja\Documents\dev\csharp\pre-renderingsrc\decoder\cpp-decoder-class\x64\Debug\

            [DllImport(decoderdll)]
            public static extern void Initialize(ref int threads);
            [DllImport(decoderdll)]
            public static extern void Create(ref string mapFile);
            [DllImport(decoderdll)]
            public static extern void SetFrame(ref double index);
            [DllImport(decoderdll)]
            public static extern void ShowCustomImage(ref string window, ref string path);
            [DllImport(decoderdll)]
            public static extern void ShowImage(ref int id, ref string window);
            [DllImport(decoderdll)]
            public static extern string GetImage(ref int id);
            [DllImport(decoderdll, EntryPoint = "GetBytes", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            public static extern string GetBytes(int id, string path);
            [DllImport(decoderdll)]
            public static extern IntPtr GetUnsigned_Bytes(int id, string path, out int bytes_count);
            [DllImport(decoderdll)]
            public static extern IntPtr GetUnsignedBytes(string path, out int bytes_count);
            [DllImport(decoderdll, EntryPoint = "testString2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
            [return: MarshalAs(UnmanagedType.LPStr)]
            public static extern string testString2();

            [DllImport(decoderdll)]
            public static extern void Destroy(ref int id);
        }

        class Programm
        {
            static string window = "Test Image OMG";
            static string tstpath1 = "E:\\users\\wanja\\Bilder\\Wallpapers\\";
            static string tstpath2 = "C:\\Users\\wanja\\Pictures\\Wallpapers\\";
            static string filepath1 = "E:\\users\\wanja\\Dokumente\\Programmieren\\C#\\pre-rendering\\src\\decoder\\files";
            static string tstimg = tstpath1 + "tst3.jpg";
            static int threads = 4;

            static void ArchivedMain(string[] args)
            {
                tstimg = "E:\\users\\wanja\\Bilder\\Wallpapers\\tstimg2.jpeg";

                // DLLWrapper.Initialize(ref threads);

                Console.WriteLine("Starting...");
                //string raw = DLLWrapper.GetBytes(threads, tstimg);
                IntPtr ptr = DLLWrapper.GetUnsignedBytes(tstimg, out int bytes_count);
                byte[] bytes = new byte[bytes_count];
                Marshal.Copy(ptr, bytes, 0, bytes_count);

                Console.WriteLine(bytes.Length);
                // char[] chars = raw;
                Console.WriteLine("Ended");
                /*
                Console.WriteLine(raw.Length.ToString());
                byte[] raw_bytes = Encoding.ASCII.GetBytes(raw);
                /* ASCII
                 * UTF-8
                 *    -7
                 *    -32
                 * Unicode
                 * BigEndianUnicode //
                Console.WriteLine(string.Format("{0}MB ({1} bytes)", (raw_bytes.Length / 1000000).ToString(), raw_bytes.Length));
                string hex = BitConverter.ToString(raw_bytes);

                File.WriteAllBytes(filepath1 + "tstbinary.bin", raw_bytes);
                // Console.WriteLine(hex);

                DLLWrapper.Destroy(ref threads);
                */
            }
        }
    }
}
