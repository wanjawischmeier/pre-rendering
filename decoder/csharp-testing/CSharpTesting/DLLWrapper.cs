using System;
using System.Runtime.InteropServices;

namespace CSharpTesting
{
    public static class DLLWrapper
    {
        const string projectpath1 = "E:\\users\\wanja\\Dokumente\\Programmieren\\C#\\pre-rendering\\";
        const string projectpath2 = "C:\\Users\\wanja\\Documents\\dev\\csharp\\pre-rendering\\";
        const string decoderdll = projectpath1 + "src\\decoder\\cpp-decoder-class\\x64\\Debug\\Decoder.dll";

        [DllImport(decoderdll)]
        public static extern IntPtr GetUnsignedBytes(string path, out int bytes_count, bool debug = false);
    }
}