using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

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
        public Vector3[] offArray;
        Vector3[] oldOffArray;
        readonly string mainPath;

        public Texture2DArray textures;
        Texture2DArray oldTextures;
        Texture2D reader;

        public Map(string path, int maxTextures)
        {
            mainPath = path;
            reader = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            offArray = new Vector3[maxTextures];

            string rawConfig = File.ReadAllText(Path.Combine(mainPath, ".mapconfig"));
            config = JsonUtility.FromJson<StandaloneMapConfig>(rawConfig);

            string sampleTexturePath = VectorToFileName(Vector3.zero);
            Texture2D texture = LoadTexture(sampleTexturePath);

            config.textureWidth = texture.width;
            config.textureHeight = texture.height;

            textures = new Texture2DArray(config.textureWidth, config.textureHeight, maxTextures, TextureFormat.RGBA32, 1, false);
            oldTextures = new Texture2DArray(config.textureWidth, config.textureHeight, maxTextures, TextureFormat.RGBA32, 1, false);
        }
        ~Map()
        {
            Object.Destroy(reader);
            Object.Destroy(textures);
            Object.Destroy(oldTextures);
        }

        public void LoadTexturesNearPosition(Vector3 position)
        {
            oldOffArray = offArray;
            Graphics.CopyTexture(textures, oldTextures);
            offArray = GetClosest(position);

            for (int i = 0; i < offArray.Length; i++)
            {
                Vector3 off = offArray[i];

                if (oldOffArray.Contains(off))
                {
                    int j = Array.IndexOf(oldOffArray, off);
                    Graphics.CopyTexture(oldTextures, j, textures, i);
                }
                else
                {
                    string texturePath = VectorToFileName(off);
                    LoadTextureToArray(texturePath, i);
                }
            }
        }

        Vector3[] GetClosest(Vector3 position)
        {
            return config.vectorOffsets
                .OrderBy(x => Vector3.Distance(position, x))
                .Take(textures.depth)
                .ToArray();
        }

        string VectorToFileName(Vector3 vector)
        {
            int index = Array.IndexOf(config.vectorOffsets, vector);
            return Path.Combine(mainPath, index.ToString().PadLeft(4, '0') + ".png");
        }

        Texture2D LoadTexture(string path)
        {
            byte[] rawTexture = File.ReadAllBytes(path);
            reader.LoadImage(rawTexture);
            return reader;
        }

        void LoadTextureToArray(string path, int index)
        {
            byte[] rawTexture = File.ReadAllBytes(path);
            reader.LoadImage(rawTexture);
            Graphics.CopyTexture(reader, 0, textures, index);
        }
    }
}