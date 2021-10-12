using System.IO;
using UnityEngine;

namespace PreRendering
{
    public static class Utility
    {
        public static Resolution EstimatePanoramaResolution(int width, int height, float fov)
        {
            Resolution res = new Resolution
            {
                width = Mathf.RoundToInt(width * (360 / fov)),
                height = Mathf.RoundToInt(height * (180 / fov))
            };
            return res;
        }

        public static Texture2D LoadTexture(string path)
        {
            byte[] rawTexture = File.ReadAllBytes(path);
            Texture2D reader = new Texture2D(0, 0);
            reader.LoadImage(rawTexture);
            return reader;
        }

        public static int GetSpiralLength(Vector3Int start, Vector3Int end, int step_size = 1)
        {
            return
                (end.x + step_size - start.x) *
                (end.y + step_size - start.y) *
                (end.z + step_size - start.z);
        }

        public static int GetSpiralLength(int range, int step_size = 1)
        {
            float length = Mathf.Pow(2 * range + step_size, 3);
            return Mathf.CeilToInt(length);
        }

        public static int GetSpiralRange(int length, int step_size = 1)
        {
            float range = (Mathf.Pow(length, 1 / 3f) - step_size) / 2f;
            return Mathf.CeilToInt(range);
        }
    }
}