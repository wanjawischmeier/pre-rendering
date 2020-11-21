using System.Runtime.InteropServices;

namespace CSharpTesting
{
    public static class DLLWrapper
    {
        const string projectpath1 = "C:\\Users\\User\\Documents\\Programmieren\\Multi-Language\\pre-rendering";
        const string projectpath2 = "C:\\Users\\wanja\\Documents\\dev\\csharp\\pre-rendering\\";
        const string decoderdll = projectpath2 + "src\\decoder\\cpp-decoder-class\\x64\\Debug\\Decoder.dll";
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
        public static extern string GetBytes(ref int id, string path);

        [DllImport(decoderdll, EntryPoint = "testString2", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string testString2();

        [DllImport(decoderdll)]
        public static extern void Destroy(ref int id);
    }
}