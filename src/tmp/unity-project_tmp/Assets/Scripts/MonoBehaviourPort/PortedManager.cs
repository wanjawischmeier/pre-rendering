using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using PreRendering;
using UnityEngine.UI;
using System.Linq;

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
    public RawImage mainImage;
    public RawImage[] images;
    public VideoPlayer[] videoPlayers;
    [Range(1, 10)]
    public int threads;
    [Range(1, 10)]
    public int bufferRadius;
    public Vector2 position;
    public int mapSize;
    public long currentIdx;
    public int availableThreads;
    public float decodingRate;
    public List<long> toDecodeView;
    public List<long> pendingView;
    public long[] inBuffer;

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
        currentIdx = position.RoundToInt().ToMapCoordinate(mapSize);
        mainImage.texture = FrameBuffer.Get(currentIdx);
        availableThreads = availabe.Count;

        toDecode.CheckAndAdd(Mathf.RoundToInt(position.x + position.y * mapSize));

        for (int w = 0; w < bufferRadius; w++)
        {
            for (int i = -w; i < w; i++)
            {
                /*
                Debug.Log(string.Format(
                    "Position: {0}, {1}\t|\tw: {2}\t|\ti: {3}", 
                    position.x.ToString(), position.y.ToString(), 
                    w.ToString(), i.ToString()
                ));
                */
                toDecode.CheckAndAdd(Mathf.RoundToInt((position.x + i) + (position.y + w) * mapSize));
                toDecode.CheckAndAdd(Mathf.RoundToInt((position.x - i) + (position.y - w) * mapSize));
                toDecode.CheckAndAdd(Mathf.RoundToInt((position.x + w) + (position.y - i) * mapSize));
                toDecode.CheckAndAdd(Mathf.RoundToInt((position.x - w) + (position.y + i) * mapSize));
            }
        }

        float rate = 0;
        foreach (DecodingThreadManager threadManager in availabe)
        {
            rate += threadManager.decodingTime;
        }

        decodingRate = rate / (float)availabe.Count;

        for (int i = 0; i < threads; i++)
        {

            if (i < images.Length) images[i].texture = videoPlayers[i].texture;
            else break;
        }

        toDecodeView = toDecode;
        pendingView = pending;
        inBuffer = FrameBuffer.keys;
    }
}

public static class Helper
{
    public static List<long> CheckAndAdd(this List<long> list, int value)
    {
        Debug.Log("Checking for: " + value.ToString());
        if (                                                    // Check if the frame is already being decoded
            !PortedManager.pending.Contains(value) && !FrameBuffer.Contains(value) && PortedManager.availabe.Count != 0
        )
        {
            // list.Add(value);
            PortedManager.availabe[0].Decode(value);
        }

        return list;
    }
    /*
    public static Vector2Int RoundToInt(this Vector2 vector)
    {
        Vector2Int rVector = new Vector2Int();

        rVector.x = Mathf.RoundToInt(vector.x);
        rVector.y = Mathf.RoundToInt(vector.y);

        return rVector;
    }
    */
}
