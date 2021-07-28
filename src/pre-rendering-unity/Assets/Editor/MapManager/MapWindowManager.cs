using System.IO;
using UnityEditor;
using UnityEngine;

namespace MapManager
{
    public class MapWindowManager : EditorWindow
    {
        [MenuItem("Assets/Maps/Import Map")]
        public static void ImportMap()
        {
            GetWindow<MapImporter>();
        }

        [MenuItem("Assets/Maps/Build All Map Bundles")]
        static void BuildMapBundles()
        {
            string mapBundleDirectory = Path.Combine(Application.streamingAssetsPath, "MapBundles");
            if (!Directory.Exists(mapBundleDirectory)) Directory.CreateDirectory(mapBundleDirectory);

            BuildPipeline.BuildAssetBundles(mapBundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        }

        [MenuItem("Assets/Maps/Inspect Map Bundle")]
        static void InspectMapBundle()
        {
            GetWindow<MapInspector>();
        }
    }
}