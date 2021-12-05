using System.Runtime.InteropServices;

namespace testing
{
    public static class Decoder
    {
#if false
        const string Root = "S:\\users\\wanja\\Dokumente\\pre-rendering\\master\\src\\libpng-decoder\\x64\\Debug\\";
#else
        const string Root = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\master\\src\\libpng-decoder\\x64\\Debug\\";
#endif

        [DllImport(Root + "libpng-decoder.dll", EntryPoint = "empty")]
        static extern void Empty();

        [DllImport(Root + "libpng-decoder.dll", EntryPoint = "initialize")]
        public static extern IntPtr Initialize(string path, int _instances);

        [DllImport(Root + "libpng-decoder.dll", EntryPoint = "release")]
        public static extern void Release();

        [DllImport(Root + "libpng-decoder.dll", EntryPoint = "read_png")]
        public static extern bool ReadPNG(string path, int index);

        public static bool LibrariesLoaded
        {
            get
            {
                try
                {
                    Empty();
                    return true;
                }
                catch (EntryPointNotFoundException)
                {
                    return false;
                }
            }
        }
    }
}
