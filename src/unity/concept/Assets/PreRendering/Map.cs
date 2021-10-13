using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace PreRendering
{
    public struct RawMapConfig
    {
        public int resolution;
        public int fclip;
        public int mx_width;
        public float[] offsets;
    }

    [Serializable]
    public struct MapConfig
    {
        public int fclip;
        public int mx_width;
        public int textureWidth;
        public int textureHeight;
        public Vector3[] vectorOffsets;
    }

    public class Map
    {
        public MapConfig config;
        readonly string mainPath;

        public Map(string path)
        {
            mainPath = path;

            string rawConfig = File.ReadAllText(Path.Combine(mainPath, ".mapconfig"));
            config = JsonUtility.FromJson<MapConfig>(rawConfig);

            string sampleTexturePath = VectorToFileName(Vector3.zero);
            Texture2D texture = Utility.LoadTexture(sampleTexturePath);

            config.textureWidth = texture.width;
            config.textureHeight = texture.height;

        }

        public void LoadTexturesNearPosition(Vector3 position)
        {
            // Graphics.CopyTexture(textures, oldTextures);
            Vector3[] temp = GetClosest(position, cacheSize);

            for (int i = temp.Length -1; i >= 0; i--)
            {
                Vector3 off = temp[i];
                
                if (decoded.ContainsKey(off))
                {
                    UnityWebRequest www = decoded[off];
                    
                    Texture2D texture = DownloadHandlerTexture.GetContent(www);
                    // Graphics.CopyTexture(texture, 0, textures, i);
                    Object.Destroy(texture);
                    decoded.Remove(off);

                    offArray[i] = off;
                }
                else if (offArray.Contains(off))
                {
                    int j = Array.IndexOf(offArray, off);
                    // Graphics.CopyTexture(oldTextures, j, textures, i);

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
    }
}