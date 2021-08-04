using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MapManagement
{
    public struct RawMapConfig
    {
        public int resolution;
        public int fclip;
        public int mx_width;
        public float[] offsets;
    }

    [System.Serializable]
    public struct StandaloneMapConfig
    {
        public int fclip;
        public int mx_width;
        public int textureWidth;
        public int textureHeight;
        public Vector3[] vectorOffsets;
    }

    public class Map
    {
        public StandaloneMapConfig config;
        string mainPath;

        public Map(string path)
        {
            mainPath = path;

            TextAsset rawConfig = Resources.LoadAll<TextAsset>(path)[0];
            config = JsonUtility.FromJson<StandaloneMapConfig>(rawConfig.text);
        }

        public void SetTexturesAtPositions(Vector3[] requests, ref Texture2DArray textures)
        {
            for (int i = 0; i < requests.Length; i++)
            {
                string texturePath = Path.Combine(mainPath, VectorToFileName(requests[i]));
                Texture2D texture = Resources.Load<Texture2D>(texturePath);
                if (texture != null) Graphics.CopyTexture(texture, 0, textures, i);
            }
        }

        public Vector3[] GetClosest(Vector3 position, int length)
        {
            return config.vectorOffsets
                .OrderBy(x => Vector3.Distance(position, x))
                .Take(length)
                .ToArray();
        }

        string VectorToFileName(Vector3 vector)
        {
            int index = Array.IndexOf(config.vectorOffsets, vector);
            return index.ToString().PadLeft(4, '0');
        }
    }
}