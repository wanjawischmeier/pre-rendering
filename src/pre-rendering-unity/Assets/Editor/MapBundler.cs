using System.IO;
using UnityEditor;
using UnityEngine;

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

public class MapCreator : EditorWindow
{
    string textureDirectory = "";
    string targetDirectory = "";
    string[] plattforms;
    bool[] checkedPlattforms;
    Vector2 scrollPos;
    bool mipEnabled;
    FilterMode filterMode;
    TextureImporterFormat textureFormat;
    enum TextureSizes
    {
        _32 = 32, _64 = 64, _128 = 128, _256 = 256, _512 = 512, _1024 = 1024, _2048 = 2048, _4096 = 4096, _8192 = 8192, _16384 = 16384
    }
    TextureSizes textureSize;

    [MenuItem("Assets/Maps/Import")]
    public static void ManageMaps()
    {
        GetWindow(typeof(MapCreator));
    }

    private void OnEnable()
    {
        plattforms = new string[]
        {
            "Standalone", "Web", "iPhone", "Android", "WebGL", "Windows Store Apps", "PS4", "XboxOne", "Nintendo Switch", "tvOS"
        };
        checkedPlattforms = new bool[]
        {
            true, false, false, true, false, false, false, false, false, false
        };
        filterMode = FilterMode.Point;
        textureFormat = TextureImporterFormat.RGBA64;
        textureSize = TextureSizes._4096;
    }

    private void OnGUI()
    {
        GUILayout.Label(textureDirectory);
        if (GUILayout.Button("Texture Directory"))
        {
            textureDirectory = EditorUtility.OpenFolderPanel("Texture Directory", textureDirectory, "");
        }
        GUILayout.Label(targetDirectory);
        if (GUILayout.Button("Target Directory"))
        {
            targetDirectory = EditorUtility.OpenFolderPanel("Target Directory", targetDirectory, "");
        }
        if (Directory.Exists(targetDirectory) && Directory.Exists(textureDirectory))
        {
            string[] imagePaths = Directory.GetFiles(textureDirectory, "*.png");

            if (imagePaths.Length == 0) EditorGUILayout.HelpBox("No Textures found in " + Path.GetFileNameWithoutExtension(textureDirectory), MessageType.Warning);
            else if (imagePaths.Length != 0)
            {
                EditorGUILayout.HelpBox(imagePaths.Length.ToString() + " Textures found in " + Path.GetFileNameWithoutExtension(textureDirectory), MessageType.Info);

                filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterMode);
                textureFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Texture Format", textureFormat);
                textureSize = (TextureSizes)EditorGUILayout.EnumFlagsField("Texture Size", textureSize);
                mipEnabled = EditorGUILayout.Toggle("Generate Mip Maps", mipEnabled);
                
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
                for (int i = 0; i < plattforms.Length; i++)
                {
                    checkedPlattforms[i] = EditorGUILayout.Toggle(plattforms[i], checkedPlattforms[i]);
                    if (plattforms[i] != "Standalone" && plattforms[i] != "Android" && checkedPlattforms[i] && textureFormat == TextureImporterFormat.RGBA64)
                    {
                        EditorGUILayout.HelpBox(textureFormat.ToString() + " might not be supported by " + plattforms[i], MessageType.Warning);
                    }
                }
                EditorGUILayout.EndScrollView();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Import Map"))
                {
                    for (int i = 0; i < imagePaths.Length; i++)
                    {
                        string filePath = Path.Combine(targetDirectory, "img" + i.ToString() + ".png");
                        string assetPath = filePath.Replace(Application.dataPath, "Assets/");
                        File.Copy(imagePaths[i], filePath);

                        AssetDatabase.ImportAsset(assetPath);
                        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);

                        importer.mipmapEnabled = mipEnabled;
                        importer.filterMode = filterMode;
                        for (int j = 0; j < plattforms.Length; j++)
                        {
                            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(plattforms[j]);
                            if (checkedPlattforms[j])
                            {
                                settings.overridden = checkedPlattforms[j];
                                settings.maxTextureSize = (int)textureSize;
                                settings.format = textureFormat;
                                importer.SetPlatformTextureSettings(settings);
                            }
                            else importer.ClearPlatformTextureSettings(plattforms[j]);
                        }
                        importer.SaveAndReimport();

                    }
                }
            }
        }
    }
}
/*
[CustomEditor(typeof(TextureLoader))]
public class PlayerEditor : Editor
{
    private TextureLoader loader;
    void OnEnable()
    {
        loader = (TextureLoader)target;
    }

    void OnDisable()
    {
        
    }

    void OnDestroy()
    {
        
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Our Custom Inspector");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Player Health");
        EditorGUILayout.TextField(loader.texPath);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextArea("This is a long text");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Player Health");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.HelpBox("Test", MessageType.Warning);
        EditorGUILayout.EnumFlagsField(loader.importMode);

        if (GUILayout.Button("TestButton"))
        {
            EditorGUILayout.HelpBox("Test", MessageType.Info);
        }
    }
}
*/