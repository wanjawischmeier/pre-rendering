using System;
using System.IO;
using UnityEngine;

namespace PreRendering
{
    [Serializable]
    public struct MapConfig
    {
        public int fClip;
        public int mxWidth;
        public Vector3[] offsets;
    }

    public class Map
    {
        public readonly int fClip;
        public readonly int mxWidth;
        public readonly Resolution resolution;
        public readonly Vector3[] offsets;
        readonly string mainPath;

        public Map(string path)
        {
            mainPath = path;

            string rawConfig = File.ReadAllText(Path.Combine(mainPath, ".mapconfig"));
            MapConfig config = JsonUtility.FromJson<MapConfig>(rawConfig);

            string sampleTexturePath = config.offsets.GetFileName(mainPath, config.offsets[0]);
            Texture2D texture = Utility.LoadTexture(sampleTexturePath);

            fClip = config.fClip;
            mxWidth = config.mxWidth;
            resolution = new Resolution() { width = texture.width, height = texture.height };
            offsets = config.offsets;
        }
    }
}