using System.Diagnostics;
using testing;

const string ImagePath = "C:\\Users\\wanja\\Documents\\dev\\pre-rendering\\renders\\testing\\jpeg-2000\\highq\\jp2";
const int warmup = 2;

string[] images = Directory.GetFiles(ImagePath, "*.jp2");
/*
IntPtr ptr = Decoder.Initialize(images[0], 5);
Console.WriteLine("Loaded: {0}", Decoder.LibrariesLoaded);
*/
long[] times = new long[images.Length];
Task[] tasks = new Task[images.Length];


var decoder = new FreeImageDecoder(ImagePath);

for (int i = 0; i < images.Length; i++)
{
    tasks[i] = Task.Run(() =>
    {
        var stopwatch = Stopwatch.StartNew();
        // bool result = Decoder.ReadPNG(images[i], 0);
        ushort[] result = decoder.Decode(images[i]);
        stopwatch.Stop();
        times[i] = stopwatch.ElapsedMilliseconds;

        Console.WriteLine("[{0}] Success: {1} (in {2}ms)", i, result, stopwatch.ElapsedMilliseconds);
    });
    tasks[i].Wait();
}
/*
int size = ImageWidth * ImageChannels;
char[] chars = new char[size];
Marshal.Copy(ptr, chars, 0, size);
*/
// Decoder.Release();

long total = 0;
foreach (long time in times[warmup..])
    total += time;

Console.WriteLine("Average: {0}ms", total / (images.Length - warmup));