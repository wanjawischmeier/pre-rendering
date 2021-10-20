using System;
using Decoder = PreRendering.Decoder;
using System.Threading.Tasks;
using System.IO;

namespace DllTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string directory = Directory.GetCurrentDirectory();
            string imagePath = Path.Combine(directory, "tstimg.png");
            int w = 8; int h = 4;

            Decoder.Initialize(imagePath, w, h);
            Decoder.ImageDecoded += Decoder_ImageDecoded;
            for (int i = 0; i < 2; i++)
            {
                int j = i;
                _ = Task.Run(() =>
                {
                    Decoder.Decode(imagePath, j);
                });
            }
            Console.ReadKey(true);
            Decoder.Deinitialize();
        }

        private static void Decoder_ImageDecoded(string path, uint[] data)
        {
            string bytestr = "";

            foreach (byte _byte in data)
            {
                bytestr += _byte.ToString() + "-";
            }
            bytestr += data.Length.ToString();

            Console.WriteLine(bytestr);
        }
    }
}
