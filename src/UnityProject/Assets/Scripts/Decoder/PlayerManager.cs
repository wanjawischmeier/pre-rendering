using UnityEngine;
using PreRendering;
using UnityEngine.Video;
using System;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public int mapSize;
    [Range(1, 10)]
    public int bufferRadius;
    [Range(1, 100)]
    public int bufferSize;
    [Range(1, 10)]
    public int threads;
    public VideoClip map;
    public VideoPlayer player;
    public RawImage image;
    // public List<long> toDecode;
    public List<long> pending;
    public List<DecodingThread> availabe;
    public Texture[] buffer;
    // public RenderTexture texture;
    // public string mainPath;
    // public string mapFile;
    // public string videoFile;
    // public int width;
    // public Map.Data data;
    public long idx;
    public long iters;

    Manager manager;

    void Start()
    {
        manager = new Manager(mapSize, bufferRadius, bufferSize, threads, map);
        // data = Map.ReadMap(mainPath + mapFile, mainPath + videoFile);
    }

    void Update()
    {
        manager.position = transform.position;

        Vector3Int conv = transform.position.FloorToInt();

        idx = Manager.Extensions.CoordinatesToIndex(conv.x, conv.y, mapSize);
        // player.frame = idx;
        // Debug.Log(player.frame.ToString() + " | " + idx.ToString());
        // image.texture = player.texture;
        image.texture = FrameBuffer.Get(idx);

        pending = Manager.pending;
        // toDecode = Manager.toDecode;
        availabe = Manager.availabe;
        iters = manager.iters;
        buffer = FrameBuffer.values;
    }
}