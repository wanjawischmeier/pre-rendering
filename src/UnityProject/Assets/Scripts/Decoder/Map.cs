using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace PreRendering
{
    public static class Map
    {
        public static Data ReadMap(string mapPath, string targetVideoPath)
        {
            byte[] bytes = File.ReadAllBytes(mapPath);
            byte[][] split = SplitArray(bytes, Convert.ToByte('\n'));

            ASCIIEncoding enc = new ASCIIEncoding();
            string json_data = enc.GetString(split[0]);
            byte[] video = split[1];

            File.WriteAllBytes(targetVideoPath, video);
            Data data = JsonUtility.FromJson<Data>(json_data);
            data.videoPath = targetVideoPath;

            return data;
        }

        static T[][] SplitArray<T>(T[] array, T seperator)
        {
            int idx = Array.IndexOf(array, seperator);

            T[] first = array.Take(idx).ToArray();
            T[] second = array.Skip(idx + 1).ToArray();

            return new T[][] { first, second };
        }

        [Serializable]
        public class Data
        {
            public int width;
            public string tstvalue;
            public string videoPath;
        }
    }
}