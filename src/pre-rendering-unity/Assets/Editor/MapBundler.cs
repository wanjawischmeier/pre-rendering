using System.IO;
using UnityEditor;


public class MapBundler
{
    [MenuItem("Assets/Build All Map Bundles")]
    static void BuildMapBundles()
    {
        string mapBundleDirectory = "Assets/MapBundles";
        if (!Directory.Exists(mapBundleDirectory)) Directory.CreateDirectory(mapBundleDirectory);
        
        BuildPipeline.BuildAssetBundles(mapBundleDirectory, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }
}
