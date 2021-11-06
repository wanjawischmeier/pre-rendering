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
            ushort a = 3254;
            ushort b = 53454;
            uint c = Decoder.Pack2(a, b);
            Decoder.Unpack2(c, out uint v0, out uint v1);
            Console.WriteLine();
            /*
            string rootPath = Directory.GetCurrentDirectory().Split(new string[] { "pre-rendering" }, System.StringSplitOptions.None)[0];
            string imagePath = Path.Combine(rootPath, "pre-rendering/master/renders/room_simple_v2_270p/0000.png");
            int w = 8; int h = 4;

            Decoder.Initialize(imagePath, w, h);
            Decoder.ImageDecoded += Decoder_ImageDecoded;
            for (int i = 0; i < 1; i++)
            {
                int j = i;
                _ = Task.Run(() =>
                {
                    Decoder.Decode(imagePath, j);
                });
            }
            Console.ReadKey(true);
            Decoder.Deinitialize();
            */
        }

        private static void Decoder_ImageDecoded(string path, ulong[] data)
        {
            string bytestr = "";
            int i = 0;

            foreach (ulong pixel in data)
            {
                string sPixel = pixel.ToString();
                sPixel = sPixel.PadLeft(20, '0');

                Decoder.Unpack(pixel, out ushort r, out ushort g, out ushort b, out ushort a);
                bytestr += string.Format("{0}\t- (P: {1},\t\tC: [{2},{3},{4},{5}])\n", i++, sPixel, r, g, b, a);
            }
            bytestr += string.Format("{0} pixels total\n", data.Length);

            Console.WriteLine(bytestr);
        }
    }
}
