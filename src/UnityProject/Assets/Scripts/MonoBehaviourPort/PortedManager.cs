using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using PreRendering;
using UnityEngine.UI;

public class PortedManager : MonoBehaviour
{
    /// <summary>
    /// The frames that need to be decoded
    /// </summary>
    public static List<long> toDecode;
    /// <summary>
    /// The frames that are currently being decoded
    /// </summary>
    public static List<long> pending;
    /// <summary>
    /// The threads that are currently available for decoding
    /// </summary>
    public static List<DecodingThreadManager> availabe;
    /// <summary>
    /// The player's position, should be set in the Update/FixedUpdate loop from the MonoBehaviour this cass instance was created in
    /// </summary>
    
    public VideoClip map;
    public GameObject decodingThread;
    public RawImage[] images;
    public VideoPlayer[] videoPlayers;
    [Range(1, 10)]
    public int threads;
    public Vector3 position;
    public int mapSize;
    public int bufferRadius;
    public List<long> toDecodeView;
    public List<long> pendingView;

    void Start()
    {
        toDecode = new List<long>();
        pending = new List<long>();
        availabe = new List<DecodingThreadManager>();
        videoPlayers = new VideoPlayer[threads];

        for (int i = 0; i < threads; i++)
        {
            GameObject thread = Instantiate(decodingThread, transform);
            thread.GetComponent<VideoPlayer>().clip = map;
            videoPlayers[i] = thread.GetComponent<VideoPlayer>();
        }
    }

    void Update()
    {
        toDecode.Clear();

        for (int w = 0; w < bufferRadius; w++)
        {
            for (int i = -w; i < w; i++)
            {
                toDecode.CheckAndAdd(Mathf.RoundToInt((position.x + i) + (position.y + w) * mapSize));
                toDecode.CheckAndAdd(Mathf.RoundToInt((position.x - i) + (position.y - w) * mapSize));
                toDecode.CheckAndAdd(Mathf.RoundToInt((position.x + w) + (position.y - i) * mapSize));
                toDecode.CheckAndAdd(Mathf.RoundToInt((position.x - w) + (position.y + i) * mapSize));
            }
        }

        for (int i = 0; i < threads; i++)
        {
            images[i].texture = videoPlayers[i].texture;
        }

        toDecodeView = toDecode;
        pendingView = pending;
    }
}

public static class ListHelper
{
    public static List<long> CheckAndAdd(this List<long> list, int value)
    {
        if (                                                    // Check if the frame is already being decoded
            !PortedManager.pending.Contains(value) && !FrameBuffer.Contains(value) && PortedManager.availabe.Count != 0
        )
        {
            list.Add(value);
        }

        return list;
    }
}
