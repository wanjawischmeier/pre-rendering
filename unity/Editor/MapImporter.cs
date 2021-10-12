using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MapManagement
{
    public class MapImporter
    {
        [MenuItem("Assets/Maps/Import")]
        static void ImportMap()
        {
            string[] plattforms = { BuildTarget.StandaloneWindows.ToString(), BuildTarget.Android.ToString() };
            
            string texturePath = EditorUtility.OpenFolderPanel("Raw Texture Directory", Application.dataPath, "");
            if (!Directory.Exists(texturePath)) return;

            string rawTargetPath = EditorUtility.SaveFolderPanel("Save Map File", Application.dataPath, Path.GetFileNameWithoutExtension(texturePath));
            string mapName = Path.GetFileName(rawTargetPath);
            string targetPath = rawTargetPath.Replace(Application.dataPath, "Assets");
            if (!Directory.Exists(targetPath)) return;
            
            DateTime start = DateTime.UtcNow;

            string[] imagePaths = Directory.GetFiles(texturePath, "*.png");
            if (imagePaths.Length == 0) return;

            string[] textFilePaths = Directory.GetFiles(texturePath, "*.mapconfig");
            if (textFilePaths.Length != 1) return;
            string configFile = File.ReadAllText(textFilePaths[0]);
            RawMapConfig config = JsonUtility.FromJson<RawMapConfig>(configFile);
            StandaloneMapConfig standaloneConfig;

            standaloneConfig.fclip = config.fclip;
            standaloneConfig.mx_width = config.mx_width;
            standaloneConfig.textureWidth = config.resolution;
            standaloneConfig.textureHeight = config.resolution / 2;
            standaloneConfig.vectorOffsets = new Vector3[config.offsets.Length / 3];
            for (int i = 0; i < config.offsets.Length / 3; i++)
            {
                standaloneConfig.vectorOffsets[i] = new Vector3(
                    config.offsets[i * 3 + 0],
                    config.offsets[i * 3 + 2],
                    config.offsets[i * 3 + 1]);
            }
            
            string standaloneConfigString = JsonUtility.ToJson(standaloneConfig);
            string standaloneConfigPath = Path.Combine(targetPath, "config.asset");
            TextAsset standaloneConfigAsset = new TextAsset(standaloneConfigString);
            AssetDatabase.CreateAsset(standaloneConfigAsset, standaloneConfigPath);
            AssetImporter configImporter = AssetImporter.GetAtPath(standaloneConfigPath);
            configImporter.assetBundleName = mapName;

            foreach (string imagePath in imagePaths)
            {
                string targetImagePath = Path.Combine(targetPath, Path.GetFileName(imagePath));
                File.Copy(imagePath, targetImagePath);
                AssetDatabase.ImportAsset(targetImagePath);
                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(targetImagePath);

                textureImporter.mipmapEnabled = false;
                textureImporter.filterMode = FilterMode.Point;

                foreach (string plattform in plattforms)
                {
                    TextureImporterPlatformSettings settings = textureImporter.GetPlatformTextureSettings(plattform);
                    settings.overridden = true;
                    settings.maxTextureSize = config.resolution;
                    settings.format = TextureImporterFormat.RGBA64;
                    textureImporter.SetPlatformTextureSettings(settings);
                }
                
                textureImporter.SaveAndReimport();
            }

            double elapsedTime = Math.Round((DateTime.UtcNow - start).TotalMinutes, 2);
            Debug.Log("Map creation took " + elapsedTime.ToString() + " Minutes");
        }

        [MenuItem("Assets/Maps/Convert To Map")]
        static void ConvertToMap()
        {
            string plattform = BuildTarget.StandaloneWindows.ToString();
            string tempGUID;

            string texturePath = EditorUtility.OpenFolderPanel("Raw Texture Directory", Application.dataPath, "");
            if (!Directory.Exists(texturePath)) return;

            string targetPath = EditorUtility.SaveFilePanel("Save Map File", Application.dataPath, Path.GetFileNameWithoutExtension(texturePath).ToLower(), "");
            string mapName = Path.GetFileName(targetPath);
            if (!Directory.Exists(Path.GetDirectoryName(targetPath))) return;

            DateTime start = DateTime.UtcNow;

            string[] imagePaths = Directory.GetFiles(texturePath, "*.png");
            if (imagePaths.Length == 0) return;

            string[] textFilePaths = Directory.GetFiles(texturePath, "*.mapconfig");
            if (textFilePaths.Length != 1) return;
            string configFile = File.ReadAllText(textFilePaths[0]);
            RawMapConfig config = JsonUtility.FromJson<RawMapConfig>(configFile);
            StandaloneMapConfig standaloneConfig;

            standaloneConfig.fclip = config.fclip;
            standaloneConfig.mx_width = config.mx_width;
            standaloneConfig.textureWidth = config.resolution;
            standaloneConfig.textureHeight = config.resolution / 2;
            standaloneConfig.vectorOffsets = new Vector3[config.offsets.Length / 3];
            for (int i = 0; i < config.offsets.Length / 3; i++)
            {
                standaloneConfig.vectorOffsets[i] = new Vector3(
                    config.offsets[i * 3 + 0],
                    config.offsets[i * 3 + 1],
                    config.offsets[i * 3 + 2]);
            }

            tempGUID = AssetDatabase.CreateFolder("Assets", "Temp");
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

            string mapBundlePath = Path.Combine(bundleDirectory, mapName.ToLower());
            File.Copy(mapBundlePath, targetPath);

            AssetBundle.UnloadAllAssetBundles(false);
            AssetDatabase.DeleteAsset(tempDir);

            double elapsedTime = Math.Round((DateTime.UtcNow - start).TotalMinutes, 2);
            Debug.Log("Map creation took " + elapsedTime.ToString() + " Minutes");
        }
    }   
}

public class ReimportMap : EditorWindow
{
    int[] resolutions;
    string[] plattforms;
    int selectedResolution = 4096;
    string texturePath;

    [MenuItem("Assets/Maps/Reimport")]
    static void Init()
    {
        GetWindow<ReimportMap>().Show();
    }

    private void OnEnable()
    {
        resolutions = new int[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };
        plattforms = new string[] { BuildTarget.StandaloneWindows.ToString(), BuildTarget.Android.ToString() };

        texturePath = EditorUtility.OpenFolderPanel("Texture Directory", Application.dataPath, "");
        if (!Directory.Exists(texturePath)) Close();
    }

    void OnGUI()
    {
        selectedResolution = EditorGUILayout.IntPopup(selectedResolution, Array.ConvertAll(resolutions, x => x.ToString()), resolutions);

        if (GUILayout.Button("Reimport"))
        {
            string mapName = Path.GetFileName(texturePath);
            string targetPath = texturePath.Replace(Application.dataPath, "Assets");
            if (!Directory.Exists(targetPath)) Close();

            DateTime start = DateTime.UtcNow;

            string[] imagePaths = Directory.GetFiles(texturePath, "*.png");
            if (imagePaths.Length == 0) Close();

            foreach (string imagePath in imagePaths)
            {
                string targetImagePath = Path.Combine(targetPath, Path.GetFileName(imagePath));
                TextureImporter textureImporter = (TextureImporter)AssetImporter.GetAtPath(targetImagePath);

                textureImporter.mipmapEnabled = false;
                textureImporter.filterMode = FilterMode.Point;

                foreach (string plattform in plattforms)
                {
                    TextureImporterPlatformSettings settings = textureImporter.GetPlatformTextureSettings(plattform);
                    settings.overridden = true;
                    settings.maxTextureSize = selectedResolution;
                    settings.format = TextureImporterFormat.RGBA64;
                    textureImporter.SetPlatformTextureSettings(settings);
                }

                textureImporter.SaveAndReimport();
            }

            double elapsedTime = Math.Round((DateTime.UtcNow - start).TotalMinutes, 2);
            Debug.Log("Map creation took " + elapsedTime.ToString() + " Minutes");
            Close();
        }
    }
}