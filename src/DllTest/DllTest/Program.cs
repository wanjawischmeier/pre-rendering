using System;
using System.Text;
using System.Runtime.InteropServices;
using PreRendering;
using Decoder = PreRendering.Decoder;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DllTest
{
    class Program
    {
        static string image_path = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\unity\\concept\\Assets\\Rendering\\Testing\\Sample1\\Main.png";

        static async Task Main(string[] args)
        {
            int w = 8; int h = 4;

            Decoder.Initialize();
            List<Task<uint[]>> tasks = new List<Task<uint[]>>();
            for (int i = 0; i < 10; i++)
            {
                int j = i;
                tasks.Add(Task.Run(() =>
                {
                    return Decoder.Decode(image_path, w, h, j);
                }));
            }
            Decoder.ImageDecoded += Decoder_ImageDecoded;
            await Task.WhenAll(tasks);
            Decoder.Deinitialize();
        }

        private static void Decoder_ImageDecoded(string path, uint[] data)
        {
            string bytestr = "";

            foreach (uint _byte in data)
            {
                bytestr += _byte.ToString() + "-";
            }
            bytestr += data.Length.ToString();

            Console.WriteLine(bytestr);
        }
    }
}
