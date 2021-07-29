using System.IO;
using UnityEditor;
using UnityEngine;

public class MapInspector : EditorWindow
{
    [MenuItem("Assets/Maps/Inspect Map Bundle")]
    static void InspectMapBundle()
    {
        GetWindow<MapInspector>();
    }

    string bundleDirectory;
    Texture2D[] textures;
    Vector2 texScroll = Vector2.zero;

    private void OnEnable()
    {
        titleContent = new GUIContent("Map Bundler");

        bundleDirectory = EditorUtility.OpenFilePanel("Target Directory", Path.Combine(Application.streamingAssetsPath, "MapBundles"), "*");
        try
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(bundleDirectory);
            textures = bundle.LoadAllAssets<Texture2D>();
        }
        catch (System.Exception)
        {
            Debug.LogError("Unable to load textures from asset bundle");
        }
    }
    
    private void OnGUI()
    {
        if (textures == null)
        {
            EditorGUILayout.HelpBox("Unable to load textures from the selected asset bundle", MessageType.Error);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Try again"))
            {
                Close();
                GetWindow<MapInspector>();
            }
            return;
        }
        if (textures.Length == 0)
        {
            EditorGUILayout.HelpBox("No textures inside the selected asset bundle", MessageType.Error);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Try again"))
            {
                Close();
                GetWindow<MapInspector>();
            }
            return;
        }

        texScroll = GUILayout.BeginScrollView(texScroll);
        foreach (Texture2D texture in textures)
        {
            EditorGUILayout.ObjectField(texture, typeof(Texture2D), false);
        }
        GUILayout.EndScrollView();
    }

    private void OnDestroy()
    {
        AssetBundle.UnloadAllAssetBundles(false);
    }
}