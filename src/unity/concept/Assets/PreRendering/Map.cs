using System;
using System.IO;
using UnityEngine;

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
        public Vector3[] vectorOffsets;
    }

    public class Map
    {
        public readonly int fclip;
        public readonly int mxWidth;
        public readonly int textureWidth;
        public readonly int textureHeight;
        public readonly Vector3[] vectorOffsets;
        readonly string mainPath;

        public Map(string path)
        {
            mainPath = path;

            string rawConfig = File.ReadAllText(Path.Combine(mainPath, ".mapconfig"));
            MapConfig config = JsonUtility.FromJson<MapConfig>(rawConfig);

            string sampleTexturePath = config.vectorOffsets.GetFileName(mainPath, Vector3.zero);
            Texture2D texture = Utility.LoadTexture(sampleTexturePath);

            fclip = config.fclip;
            mxWidth = config.mx_width;
            textureWidth = texture.width;
            textureHeight = texture.height;
            vectorOffsets = config.vectorOffsets;
        }
    }
}