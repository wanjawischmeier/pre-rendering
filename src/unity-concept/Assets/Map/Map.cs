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

    [Serializable]
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
        readonly string mainPath;

        public Map(string path)
        {
            mainPath = path;

            string rawConfig = File.ReadAllText(Path.Combine(mainPath, ".mapconfig"));
            config = JsonUtility.FromJson<StandaloneMapConfig>(rawConfig);

            string sampleTexturePath = VectorToFileName(mainPath, Vector3.zero);
            Texture2D texture = LoadTexture(sampleTexturePath);

            config.textureWidth = texture.width;
            config.textureHeight = texture.height;
        }

        public void SetTexturesAtPositions(Vector3[] requests, ref Texture2DArray textures)
        {
            for (int i = 0; i < requests.Length; i++)
            {
                string texturePath = VectorToFileName(mainPath, requests[i]);
                Texture2D texture = LoadTexture(texturePath);
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

        string VectorToFileName(string path, Vector3 vector)
        {
            int index = Array.IndexOf(config.vectorOffsets, vector);
            return Path.Combine(path, index.ToString().PadLeft(4, '0') + ".png");
        }

        Texture2D LoadTexture(string path)
        {
            byte[] rawTexture = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            texture.LoadImage(rawTexture);
            return texture;
        }
    }
}