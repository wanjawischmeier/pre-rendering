using System;
using System.IO;
using UnityEngine;

namespace PreRendering
{
    [Serializable]
    public struct MapConfig
    {
        public int nClip, fClip, mxWidth;
        public Vector3[] offsets;
    }

    public class Map
    {
        public readonly int nClip, fClip, mxWidth;
        public readonly Resolution resolution;
        public readonly Vector3[] offsets;
        readonly string mainPath;

        public bool Valid
        {
            get
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    string offsetPath = offsets.GetFileName(mainPath, offsets[i]);
                    if (!File.Exists(offsetPath)) return false;
                }
                return true;
            }
        }

        public Map(string path)
        {
            mainPath = path;
            string rawConfigPath = Path.Combine(mainPath, ".mapconfig");
            if (!File.Exists(rawConfigPath)) throw new Exception("There is no '.mapconfig' file inside the specified directory");

            string rawConfig = File.ReadAllText(rawConfigPath);
            MapConfig config = JsonUtility.FromJson<MapConfig>(rawConfig);

            offsets = config.offsets;
            if (!Valid) throw new Exception("The map file is incomplete or corrupt");

            string sampleTexturePath = config.offsets.GetFileName(mainPath, config.offsets[0]);
            Texture2D texture = Utility.LoadTexture(sampleTexturePath);

            nClip = config.nClip;
            fClip = config.fClip;
            mxWidth = config.mxWidth;
            resolution = new Resolution() { width = texture.width, height = texture.height };
        }
    }
}