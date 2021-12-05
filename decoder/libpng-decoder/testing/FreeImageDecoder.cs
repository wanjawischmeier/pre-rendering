using FreeImageAPI;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace testing
{
    public class FreeImageDecoder
    {
        FIBITMAP dib;

        public FreeImageDecoder(string path, int channels = 4)
        {
            dib = new FIBITMAP();
        }

        ~FreeImageDecoder()
        {
            dib.SetNull();
        }

        public ushort[] Decode(string path, out long elapsedDecoding, out long elapsedMarshalling, int channels = 4)
        {
            if (!dib.IsNull)
                FreeImage.Unload(dib);

            var decoding = Stopwatch.StartNew();
            dib = FreeImage.Load(FREE_IMAGE_FORMAT.FIF_JP2, path, FREE_IMAGE_LOAD_FLAGS.DEFAULT);
            decoding.Stop();

            var marshalling = Stopwatch.StartNew();
            uint width = FreeImage.GetWidth(dib);
            uint height = FreeImage.GetHeight(dib);
            uint scanWidth = FreeImage.GetPitch(dib);
            ushort[] data = new ushort[width * height * channels];

            GCHandle gch = GCHandle.Alloc(data, GCHandleType.Pinned);
            FreeImage.ConvertToRawBits(
                gch.AddrOfPinnedObject(),
                dib,
                (int)scanWidth,
                64,
                FreeImage.FI_RGBA_RED_MASK,
                FreeImage.FI_RGBA_GREEN_MASK,
                FreeImage.FI_RGBA_BLUE_MASK,
                false);
            gch.Free();
            marshalling.Stop();

            elapsedDecoding = decoding.ElapsedMilliseconds;
            elapsedMarshalling = marshalling.ElapsedMilliseconds;
            return data;
        }

        public ushort[] Decode(string path)
        {
            return Decode(path, out _, out _);
        }
    }
}
