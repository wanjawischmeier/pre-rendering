using System.Runtime.InteropServices;

namespace CSharpTesting
{
    public static class DLLWrapper
    {
        const string decoderdll = "C:\\Users\\User\\Documents\\Programmieren\\Multi-Language\\pre-rendering\\src\\Decoder\\Decoder\\x64\\Debug\\Decoder.dll";

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
        public static extern void Destroy(ref int id);
    }
}