using UnityEngine;
using UnityEngine.Video;
using Newtonsoft.Json;
using System.IO;
using PreRendering;

public class MapSaving : MonoBehaviour
{
    public VideoClip clip;
    /// <summary>
    /// Very important:
    ///   1. Max Size to '8192'
    ///   2. Compression to 'None'
    ///   (In image import settings)
    /// </summary>
    public Texture2D image;
    public byte[] outArr;
    public Texture2D output;
    public bool loaded;
    void Start()
    {
        //clip.SaveToJson();
        image.SaveToJson();

        Debug.Log("Saved!");
        
        //string str = File.ReadAllText(string.Format("{0}/{1}.ods_map", Path.GetDirectoryName(clip.originalPath), clip.name));
        string str = File.ReadAllText(string.Format("{0}/{1}.ods_map", "Assets/Maps", image.name));

        MapCombined mapCombined = JsonConvert.DeserializeObject<MapCombined>(str);

        outArr = mapCombined.image;
        loaded = output.LoadImage(mapCombined.image);
    }
}

public static class Mapsaver
{
    public static MapCombined SaveToJson(this VideoClip clip)
    {
        MapCombined map = new MapCombined();
        string path = Path.GetDirectoryName(clip.originalPath);
        map.name = clip.name;
        map.path = clip.originalPath;
        //map.video = File.ReadAllBytes(clip.originalPath);
        string map_json = JsonConvert.SerializeObject(map);

        File.WriteAllText(string.Format("{0}/{1}.ods_map", path, clip.name), map_json);
        
        //return map_json;
        return map;
    }

    public static MapCombined SaveToJson(this Texture2D image)
    {
        MapCombined map = new MapCombined();
        string path = "Assets/Maps";
        map.name = image.name;
        map.path = path;
        map.image = image.EncodeToJPG();
        //map.video = File.ReadAllBytes(clip.originalPath);
        string map_json = JsonConvert.SerializeObject(map);

        File.WriteAllText(string.Format("{0}/{1}.ods_map", map.path, map.name), map_json);

        //return map_json;
        return map;
    }
}

[System.Serializable]
public class MapCombined
{
    //public VideoClip videoClip { get; set; }

    public string name;
    public string path;
    //public byte[] video;
    public byte[] image;
}