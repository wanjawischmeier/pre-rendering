using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PreRendering
{
    [Serializable]
    public struct MapConfig
    {
        public float nclip, fclip, blockWidth, blockHeight;
        public int chunkWidth, chunkColumns, chunkRows, channelBlocks;
    }

    public static class Map
    {
        public static float nclip, fclip, blockWidth, blockHeight;
        public static int chunkWidth, chunkColumns, chunkRows, channelBlocks;

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

            config.Validate();
            nclip = config.nclip;
            fclip = config.fclip;
            blockWidth = config.blockWidth;
            blockHeight = config.blockHeight;
            chunkWidth = config.chunkWidth;
            chunkColumns = config.chunkColumns;
            chunkRows = config.chunkRows;
            channelBlocks = config.channelBlocks;
        }

        private static void Validate(this MapConfig config)
        {
            var missingAttributes = new List<string>();

            var fieldValues = config.GetType()
                     .GetFields()
                     .Select(field => field.GetValue(config))
                     .ToList();

            foreach (var field in fieldValues)
                if ((float)field == 0)
                    missingAttributes.Add($"({field.GetType().Name}) {nameof(field)}");

            var errorLog = new StringBuilder();

            if (missingAttributes.Count > 0)
            {
                errorLog.AppendLine("Missing Attributes:");

                foreach (string missingAttribute in missingAttributes)
                    errorLog.AppendLine(missingAttribute);

                throw new Exception(errorLog.ToString(), new Exception(MapError + "Certain values are missing."));
            }
        }
    }
}