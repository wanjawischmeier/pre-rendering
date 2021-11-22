using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PreRendering
{
    [Serializable]
    public struct MapConfig
    {
        public float nClip, fClip;
        public int mxWidth;
        public Vector3[] offsets;
    }

    public static class Map
    {
        public static float nClip, fClip;
        public static int mxWidth;
        public static Resolution resolution;
        public static Vector3[] offsets;

        private static string mainPath;
        private const string MapError = "The map file is incomplete or corrupt. ";

        public static void LoadFromPath(string path)
        {
            mainPath = path;
            string rawConfigPath = Path.Combine(mainPath, ".mapconfig");
            if (!File.Exists(rawConfigPath)) throw new Exception($"There is no '.mapconfig' file under {rawConfigPath}");

            string rawConfig = File.ReadAllText(rawConfigPath);

            MapConfig config;

            try
            {
                config = JsonUtility.FromJson<MapConfig>(rawConfig);
            }
            catch (ArgumentException e)
            {
                throw new Exception(e.Message, new Exception(MapError + "Unable to parse the configuration file."));
            }

            nClip = config.nClip;
            fClip = config.fClip;
            mxWidth = config.mxWidth;
            offsets = config.offsets;

            Validate();

            string sampleTexturePath = GetFileName(config.offsets[0]);
            Texture2D texture = LoadTexture(sampleTexturePath);

            resolution = new Resolution() { width = texture.width, height = texture.height };
        }

        private static void Validate()
        {
            var missingAttributes = new List<string>();
            var missingFiles = new List<string>();

            if (nClip == 0) missingAttributes.Add("(float) nClip");
            if (fClip == 0) missingAttributes.Add("(float) fClip");
            if (mxWidth == 0) missingAttributes.Add("(int) mxWidth");

            for (int i = 0; i < offsets.Length; i++)
            {
                string offsetPath = GetFileName(offsets[i]);
                if (!File.Exists(offsetPath))
                    missingFiles.Add(Path.GetFileName(offsetPath));
            }

            var errorLog = "";

            if (missingAttributes.Count > 0)
            {
                errorLog += "\nMissing Attributes:\n";

                foreach (string missingAttribute in missingAttributes)
                {
                    errorLog += missingAttribute;
                    errorLog += "\n";
                }
            }

            if (missingFiles.Count > 0)
            {
                errorLog += "\nMissing Files:\n";

                foreach (string missingFile in missingFiles)
                {
                    errorLog += missingFile;
                    errorLog += "\n";
                }
            }

            if (errorLog != "")
                throw new Exception(errorLog, new Exception(MapError + "Certain values are missing."));
        }

        /// <summary>
        /// Get a file name for a vector, based on a root directory.
        /// The vector has to be contained inside the vector array this method extends from.
        /// </summary>
        public static string GetFileName(Vector3 vector)
        {
            int index = Array.IndexOf(offsets, vector);
            return GetFileName(mainPath, index);
        }

        public static string GetFileName(string path, int index)
        {
            return Path.Combine(path, index.ToString().PadLeft(4, '0') + ".png");
        }

        /// <summary>
        /// Loads an image into a texture.
        /// !IMPORTANT! The returned texture will always be in the RGBA32 format.
        /// </summary>
        /// <param name="path">The path of the image file</param>
        public static Texture2D LoadTexture(string path)
        {
            byte[] rawTexture = File.ReadAllBytes(path);
            var reader = new Texture2D(0, 0);
            reader.LoadImage(rawTexture);
            return reader;
        }
    }
}