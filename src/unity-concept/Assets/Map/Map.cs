using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
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
        Dictionary<AsyncOperation, Tuple<int, UnityWebRequest>> pending;
        Dictionary<int, Vector3> pendingVectors;
        readonly string mainPath;

        public Texture2DArray textures;
        Texture2DArray oldTextures;
        Texture2D reader;

        public Map(string path, int maxTextures)
        {
            mainPath = path;
            reader = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            offArray = new Vector3[maxTextures];
            pending = new Dictionary<AsyncOperation, Tuple<int, UnityWebRequest>>();
            pendingVectors = new Dictionary<int, Vector3>();

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
                else if (!pendingVectors.ContainsKey(i))
                {
                    string texturePath = VectorToFileName(off);

                    UnityWebRequest www = UnityWebRequestTexture.GetTexture(texturePath);
                    var asyncOp = www.SendWebRequest();

                    pending.Add(asyncOp, new Tuple<int, UnityWebRequest>(i, www));
                    pendingVectors.Add(i, off);
                    asyncOp.completed += OnImageDecoded;
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


        // Based on: https://stackoverflow.com/a/53770838/13215204
        void OnImageDecoded(AsyncOperation obj)
        {
            Tuple<int, UnityWebRequest> data = pending[obj];
            int index = data.Item1;
            UnityWebRequest www = data.Item2;

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Graphics.CopyTexture(texture, 0, textures, index);
                Object.Destroy(texture);
            }

            pending.Remove(obj);
            pendingVectors.Remove(index);
        }
    }
}