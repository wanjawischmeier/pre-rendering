using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class MatToTexture : MonoBehaviour
{
    const string builddir1 = "E:\\users\\wanja\\Dokumente\\Programmieren\\C#\\pre-rendering\\";
    const string builddir2 = "C:\\Users\\wanja\\Documents\\dev\\csharp\\pre-rendering\\";
    // const string devbuild = builddir2 + "src\\decoder\\cpp-decoder-class\\x64\\Debug\\Decoder.dll";
    const string devbuild = "Assets/Plugins/_ecoder 2.dll";

    // static
    /*
    [DllImport(devbuild, EntryPoint = "setFrame")]
    public static extern bool SetFrame(int id, int frame);
    [DllImport(devbuild, EntryPoint = "getFrame")]
    public static extern IntPtr GetFrame(int id, int frame, ref int bytes_count);
    [DllImport(devbuild, EntryPoint = "threads")]
    public static extern int Threads();
    [DllImport(devbuild, EntryPoint = "loaded")]
    public static extern int Loaded();
    [DllImport(devbuild, EntryPoint = "getUnsignedBytes")]
    public static extern IntPtr GetUnsignedBytes(string path, out int bytes_count, bool debug = false);
    */

    // object-oriented
    [DllImport(devbuild, EntryPoint = "initialize")]
    public static extern bool Initialize(string path, int res_x, int res_y, int threads, int col_channels = 3);
    [DllImport(devbuild, EntryPoint = "decode")]
    public static extern IntPtr Decode(int frame);
    [DllImport(devbuild, EntryPoint = "release")]
    public static extern void Release();

    public string[] videos;
    public string[] project_paths;
    public int system_id;
    public int video_id;

    public int threads;
    public bool loaded;

    public int[] frames;

    public Utility.Image.Dimensions resolution;
    public Texture2D[] textures;

    void Start()
    {
        string video_path = string.Format("{0}\\src\\unity-project\\Assets\\Videos\\{1}.mp4", project_paths[system_id], videos[video_id]);

        // IntPtr ptr = GetUnsignedBytes(image_path, out int bytes_count);

        // texture = ptr.ToTexture2D(Utility.Image.Presets.FULL_HD);
        textures = new Texture2D[threads];

        loaded = Initialize(video_path, resolution.width, resolution.height, threads, resolution.channels);
        /*
        Debug.Log(Threads());
        int vid_bytes_count = 0;

        int thread;
        thread = 0;
        vid_ptr = GetFrame(thread, frames[thread], ref vid_bytes_count);
        Debug.Log(vid_bytes_count);
        textures[thread] = vid_ptr.ToTexture2D(Utility.Image.Presets.FULL_HD);
        
        thread = 1;
        vid_ptr = GetFrame(thread, frames[thread], ref vid_bytes_count);
        Debug.Log(vid_bytes_count);
        textures[thread] = vid_ptr.ToTexture2D(Utility.Image.Presets.FULL_HD);
        */
        /*
        IntPtr vid_ptr;

        vid_ptr = Decode(frames[0]);
        textures[0] = vid_ptr.ToTexture2D(resolution);
        */
        Release();
    }
}

public static class Utility
{
    public class Image
    {
        [Serializable]
        public struct Dimensions
        {
            public int width; public int height; public int channels;
            public int total
            {
                get { return this.width * this.height * this.channels; }
            }
        }

        public struct Presets
        {
            public static Dimensions FULL_HD =          new Dimensions() { width = 1920, height = 1080, channels = 3 };
            public static Dimensions FULL_HD_ALPHA =    new Dimensions() { width = 1920, height = 1080, channels = 4 };
            public static Dimensions HD =               new Dimensions() { width = 1280, height = 720,  channels = 3 };
            public static Dimensions HD_ALPHA =         new Dimensions() { width = 1280, height = 720,  channels = 4 };
        }
        
    }

    public static Texture2D ToTexture2D(this IntPtr ptr, Image.Dimensions resolution)
    {
        byte[] bytes = new byte[resolution.total];
        Marshal.Copy(ptr, bytes, 0, resolution.total);
        
        Texture2D texture = new Texture2D(resolution.width, resolution.height);

        Color32[] colorArray = new Color32[bytes.Length / resolution.channels];

        for (int i, x = 0; x < resolution.width; x++)
        {
            for (int y = 0; y < resolution.height; y++)
            {
                i = (x * resolution.channels) + (y * resolution.channels) * resolution.width;

                var color = new Color32(
                    bytes[i + 2], bytes[i + 1],
                    bytes[i + 0], 255 // bytes[i + 3]
                );

                colorArray[x + (resolution.height - y - 1) * resolution.width] = color;
            }
        }

        texture.SetPixels32(colorArray);
        texture.Apply();

        return texture;
    }
}