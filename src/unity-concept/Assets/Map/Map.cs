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
        public Dictionary<AsyncOperation, Tuple<Vector3, UnityWebRequest>> pending;
        public Dictionary<Vector3, UnityWebRequest> decoded;
        public Vector3[] offArray;
        readonly string mainPath;
        readonly int cacheSize;
        readonly int decodingThreads;

        public Texture2DArray textures;
        readonly Texture2DArray oldTextures;
        readonly Texture2D reader;

        public Map(string path, int cacheSize, int decodingThreads)
        {
            mainPath = path;
            this.cacheSize = cacheSize;
            this.decodingThreads = decodingThreads;

            reader = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            pending = new Dictionary<AsyncOperation, Tuple<Vector3, UnityWebRequest>>();
            decoded = new Dictionary<Vector3, UnityWebRequest>();
            offArray = new Vector3[cacheSize];

            string rawConfig = File.ReadAllText(Path.Combine(mainPath, ".mapconfig"));
            config = JsonUtility.FromJson<StandaloneMapConfig>(rawConfig);

            string sampleTexturePath = VectorToFileName(Vector3.zero);
            Texture2D texture = LoadTexture(sampleTexturePath);

            config.textureWidth = texture.width;
            config.textureHeight = texture.height;

            textures = new Texture2DArray(config.textureWidth, config.textureHeight, cacheSize, TextureFormat.RGBA32, 1, false);
            oldTextures = new Texture2DArray(config.textureWidth, config.textureHeight, cacheSize, TextureFormat.RGBA32, 1, false);
        }
        ~Map()
        {
            Object.Destroy(reader);
            Object.Destroy(textures);
            Object.Destroy(oldTextures);
        }

        public void LoadTexturesNearPosition(Vector3 position)
        {
            Graphics.CopyTexture(textures, oldTextures);
            Vector3[] temp = GetClosest(position, cacheSize);

            for (int i = 0; i < temp.Length; i++)
            {
                Vector3 off = temp[i];
                
                if (decoded.ContainsKey(off))
                {
                    UnityWebRequest www = decoded[off];
                    
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    Graphics.CopyTexture(texture, 0, textures, i);
                    Object.Destroy(texture);
                    decoded.Remove(off);

                    offArray[i] = off;
                }
                else if (offArray.Contains(off))
                {
                    int j = Array.IndexOf(offArray, off);
                    Graphics.CopyTexture(oldTextures, j, textures, i);

                    offArray[i] = off;
                }

                else if (pending.Count < decodingThreads && !IsPending(off))
                {
                    string texturePath = VectorToFileName(off);

                    // Based on: https://stackoverflow.com/a/53770838/13215204
                    UnityWebRequest www = UnityWebRequestTexture.GetTexture(texturePath);
                    var asyncOp = www.SendWebRequest();

                    pending.Add(asyncOp, new Tuple<Vector3, UnityWebRequest>(off, www));
                    asyncOp.completed += OnImageDecoded;
                }

                if (decoded.Count > decodingThreads)
                    decoded.Remove(decoded.Take(1).ToArray()[0].Key);
            }
        }

        void OnImageDecoded(AsyncOperation obj)
        {
            Tuple<Vector3, UnityWebRequest> data = pending[obj];

            if (data.Item2.result == UnityWebRequest.Result.Success)
                decoded.Add(data.Item1, data.Item2);

            pending.Remove(obj);
        }

        Vector3[] GetClosest(Vector3 position, int amount)
        {
            return config.vectorOffsets
                .OrderBy(x => Vector3.Distance(position, x))
                .Take(amount)
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

        bool IsPending(Vector3 vector)
        {
            return pending.Values.Select(
                (Tuple<Vector3, UnityWebRequest> value) =>
                {
                    return value.Item1;
                })
                .Contains(vector);
        }
    }
}