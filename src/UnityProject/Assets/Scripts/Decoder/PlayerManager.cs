using UnityEngine;
using PreRendering;
using UnityEngine.Video;
using System;

public class PlayerManager : MonoBehaviour
{
    public int mapSize;
    [Range(1, 10)]
    public int bufferRadius;
    [Range(1, 10)]
    public int threads;
    public VideoClip map;
    public string mainPath;
    public string mapFile;
    public string videoFile;
    public Map.Data data;

    Manager manager;

    void Start()
    {
        // manager = new Manager(mapSize, bufferRadius, transform, threads, map);
        data = Map.ReadMap(mainPath + mapFile, mainPath + videoFile);
    }

    void Update()
    {
        
    }
}