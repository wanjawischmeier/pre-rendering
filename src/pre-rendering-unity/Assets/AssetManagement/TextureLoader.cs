using System.IO;
using UnityEngine;
using UnityEditor;

// [RequireComponent(typeof(BufferManager))]
public class TextureLoader : MonoBehaviour
{
    public ImportMode importMode;

    public string texPath;
    public Texture2D[] rawTexArray;
    BufferManager manager;

    void Start()
    {
        // manager = GetComponent<BufferManager>();
        string[] imageFiles;

        switch (importMode)
        {
            case ImportMode.FromPath:
                if (File.Exists(Application.dataPath + "\\DataPath.txt")) texPath = File.ReadAllText(Application.dataPath + "\\DataPath.txt");
                if (!Directory.Exists(texPath)) Debug.LogError("Invalid file path");

                imageFiles = Directory.GetFiles(texPath, "*.png");
                rawTexArray = new Texture2D[imageFiles.Length];
                // manager.offArray = new Vector3[imageFiles.Length];

                for (int i = 0; i < imageFiles.Length; i++)
                {
                    rawTexArray[i] = new Texture2D(0, 0);
                    rawTexArray[i].LoadImage(File.ReadAllBytes(imageFiles[i]));

                    string[] split = Path.GetFileNameWithoutExtension(imageFiles[i])
                        .Split('_');
                    /*manager.offArray[i] = new Vector3(
                        float.Parse(split[0]),
                        float.Parse(split[1]),
                        float.Parse(split[2]));*/
                }
                break;
            case ImportMode.FromResources:
                imageFiles = Directory.GetFiles(texPath, "*.png");
                rawTexArray = new Texture2D[imageFiles.Length];
                // manager.offArray = new Vector3[imageFiles.Length];

                for (int i = 0; i < imageFiles.Length; i++)
                {
                    string[] plattforms = new string[] { "Standalone", "Android" };
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(imageFiles[i]);
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Point;
                    foreach (string plattform in plattforms)
                    {
                        importer.ClearPlatformTextureSettings(plattform);
                        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(plattform);
                        settings.overridden = true;
                        settings.maxTextureSize = 4096;
                        settings.format = TextureImporterFormat.RGBA64;
                        importer.SetPlatformTextureSettings(settings);
                    }
                    importer.SaveAndReimport();

                    rawTexArray[i] = (Texture2D)AssetDatabase.LoadAssetAtPath(imageFiles[i], typeof(Texture2D));
                }

                break;
            case ImportMode.FromMapBundle:
                AssetBundle bundle = AssetBundle.LoadFromFile(texPath);
                rawTexArray = bundle.LoadAllAssets<Texture2D>();

                break;
            case ImportMode.Manual:
                if (rawTexArray.Length != 0)
                {
                    // manager.offArray = new Vector3[rawTexArray.Length];

                    for (int i = 0; i < rawTexArray.Length; i++)
                    {
                        string[] split = rawTexArray[i].name
                            .Split('_');
                        /*manager.offArray[i] = new Vector3(
                            float.Parse(split[0]),
                            float.Parse(split[1]),
                            float.Parse(split[2]));*/
                    }
                }
                break;
            default:
                break;
        }
        /*
        manager.texArray = new Texture2DArray(rawTexArray[0].width, rawTexArray[0].height, rawTexArray.Length, rawTexArray[0].format, false);

        for (int i = 0; i < rawTexArray.Length; i++)
        {

            Graphics.CopyTexture(rawTexArray[i], 0, 0, manager.texArray, i, 0);
        }*/
    }
}

public enum ImportMode
{
    FromPath, FromResources, FromMapBundle, Manual
}