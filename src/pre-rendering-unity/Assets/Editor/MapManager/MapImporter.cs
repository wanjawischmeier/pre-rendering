using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class MapImporter
{
    struct RawMapConfig
    {
        public int resolution;
        public int fclip;
        public float[] offsets;
    }

    struct StandaloneMapConfig
    {
        public int fclip;
        public Vector3[] vectorOffsets;
    }

    [MenuItem("Assets/Maps/Convert To Map")]
    static void ConvertToMap()
    {
        string plattform = BuildTarget.StandaloneWindows.ToString();
        string tempGUID;

        string texturePath = EditorUtility.OpenFolderPanel("Raw Texture Directory", Application.streamingAssetsPath, "");
        if (!Directory.Exists(texturePath)) return;

        string targetPath = EditorUtility.SaveFilePanel("Save Map File", Application.streamingAssetsPath, Path.GetFileNameWithoutExtension(texturePath), "");
        string mapName = Path.GetFileNameWithoutExtension(targetPath);
        if (!Directory.Exists(Path.GetDirectoryName(texturePath))) return;

        string[] imagePaths = Directory.GetFiles(texturePath, "*.png");
        if (imagePaths.Length == 0) return;

        string[] textFilePaths = Directory.GetFiles(texturePath, "*.mapconfig");
        if (textFilePaths.Length != 1) return;
        string configFile = File.ReadAllText(textFilePaths[0]);
        RawMapConfig config = JsonUtility.FromJson<RawMapConfig>(configFile);
        StandaloneMapConfig standaloneConfig;

        standaloneConfig.fclip = config.fclip;
        standaloneConfig.vectorOffsets = new Vector3[config.offsets.Length / 3];
        for (int i = 0; i < config.offsets.Length / 3; i++)
        {
            standaloneConfig.vectorOffsets[i] = new Vector3(
                config.offsets[i * 3 + 0],
                config.offsets[i * 3 + 1],
                config.offsets[i * 3 + 2]);
        }
        foreach (Vector3 vector in standaloneConfig.vectorOffsets)
        {
            Debug.Log(vector);
        }

        tempGUID = AssetDatabase.CreateFolder("Assets/StreamingAssets", "Temp");
        string tempDir = AssetDatabase.GUIDToAssetPath(tempGUID);

        string standaloneConfigString = JsonUtility.ToJson(standaloneConfig);
        string standaloneConfigPath = Path.Combine(tempDir, "config.asset");
        TextAsset standaloneConfigAsset = new TextAsset(standaloneConfigString);
        AssetDatabase.CreateAsset(standaloneConfigAsset, standaloneConfigPath);
        AssetImporter configImporter = AssetImporter.GetAtPath(standaloneConfigPath);
        configImporter.assetBundleName = mapName;

        foreach (string imagePath in imagePaths)
        {
            string targetImagePath = Path.Combine(tempDir, Path.GetFileName(imagePath));
            File.Copy(imagePath, targetImagePath);
            AssetDatabase.ImportAsset(targetImagePath);
            TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(targetImagePath);

            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;

            TextureImporterPlatformSettings settings = textureImporter.GetPlatformTextureSettings(plattform);
            settings.overridden = true;
            settings.maxTextureSize = config.resolution;
            settings.format = TextureImporterFormat.RGBA64;
            textureImporter.SetPlatformTextureSettings(settings);

            textureImporter.assetBundleName = mapName;
            textureImporter.SaveAndReimport();
        }

        tempGUID = AssetDatabase.CreateFolder(tempDir, mapName + "RawBundle" + plattform);
        string bundleDirectory = AssetDatabase.GUIDToAssetPath(tempGUID);
        
        Enum.TryParse(plattform, out BuildTarget target);
        BuildPipeline.BuildAssetBundles(bundleDirectory, BuildAssetBundleOptions.None, target);

        AssetBundle.UnloadAllAssetBundles(false);
        // AssetDatabase.DeleteAsset(tempDir);
    }
    /*
    private void OnGUI()
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
    }*/
}