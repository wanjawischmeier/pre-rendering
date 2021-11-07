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

    public class Map
    {
        public readonly float nClip, fClip;
        public readonly int mxWidth;
        public readonly Resolution resolution;
        public readonly Vector3[] offsets;
        readonly string mainPath;

        const string mapError = "The map file is incomplete or corrupt. ";

        public Map(string path)
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
                throw new Exception(e.Message, new Exception(mapError + "Unable to parse the configuration file."));
            }

            nClip = config.nClip;
            fClip = config.fClip;
            mxWidth = config.mxWidth;
            offsets = config.offsets;
            Verify();

            string sampleTexturePath = GetFileName(config.offsets[0]);
            Texture2D texture = Utility.LoadTexture(sampleTexturePath);

            resolution = new Resolution() { width = texture.width, height = texture.height };
        }

        private void Verify()
        {
            List<string> missingAttributes = new List<string>();
            List<string> missingFiles = new List<string>();

            if (nClip == 0) missingAttributes.Add("(float) nClip");
            if (fClip == 0) missingAttributes.Add("(float) fClip");
            if (mxWidth == 0) missingAttributes.Add("(int) mxWidth");

            for (int i = 0; i < offsets.Length; i++)
            {
                string offsetPath = GetFileName(offsets[i]);
                if (!File.Exists(offsetPath))
                    missingFiles.Add(Path.GetFileName(offsetPath));
            }

            string errorLog = "";

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
                throw new Exception(errorLog, new Exception(mapError + "Certain values are missing."));
        }

        /// <summary>
        /// Get a file name for a vector, based on a root directory.
        /// The vector has to be contained inside the vector array this method extends from.
        /// </summary>
        public string GetFileName(Vector3 vector)
        {
            int index = Array.IndexOf(offsets, vector);
            return Utility.GetFileName(mainPath, index);
        }
    }

    /// <summary>
    /// Contains all static helper functions used inside the 'PreRendering' namespace.
    /// </summary>
    public static partial class Utility
    {
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
            Texture2D reader = new Texture2D(0, 0);
            reader.LoadImage(rawTexture);
            return reader;
        }
    }
}