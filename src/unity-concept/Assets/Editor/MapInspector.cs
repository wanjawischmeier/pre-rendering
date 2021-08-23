using System.IO;
using UnityEditor;
using UnityEngine;

public class MapInspector : EditorWindow
{
    [MenuItem("Assets/Maps/Test")]
    static void Test()
    {
        Vector3 vector = Vector3.up;
        Debug.Log(vector.ToString());
    }

    [MenuItem("Assets/Maps/Inspect Map Bundle")]
    static void InspectMapBundle()
    {
        GetWindow<MapInspector>();
    }

    string bundleDirectory;
    TextAsset config;
    Texture2D[] textures;
    Vector2 texScroll = Vector2.zero;

    private void OnEnable()
    {
        titleContent = new GUIContent("Map Bundler");

        bundleDirectory = EditorUtility.OpenFilePanel("Target Directory", Path.Combine(Application.streamingAssetsPath, "MapBundles"), "*");

        EditorUtility.DisplayProgressBar("Loading Map Bundle", "Loading Bundle From File", 0.1f);
        try
        {
            EditorUtility.DisplayProgressBar("Loading Map Bundle", "Loading Bundle From File", 0.2f);
            AssetBundle bundle = AssetBundle.LoadFromFile(bundleDirectory);
            EditorUtility.DisplayProgressBar("Loading Map Bundle", "Loading Text Assets (Config File)", 0.7f);
            config = bundle.LoadAllAssets<TextAsset>()[0];
            EditorUtility.DisplayProgressBar("Loading Map Bundle", "Loading Textures", 0.8f);
            textures = bundle.LoadAllAssets<Texture2D>();
        }
        catch (System.Exception)
        {
            Debug.LogError("Unable to load textures from asset bundle");
        }
        EditorUtility.ClearProgressBar();
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
        GUILayout.Label("Config:");
        GUILayout.TextArea(config.text);
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