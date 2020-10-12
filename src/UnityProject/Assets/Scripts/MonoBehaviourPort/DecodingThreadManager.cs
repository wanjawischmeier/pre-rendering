using PreRendering;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

public class DecodingThreadManager : MonoBehaviour
{
    VideoPlayer player;
    bool decoding = false;
    long frameIdx = 0;

    void Start()
    {
        player = GetComponent<VideoPlayer>();
        player.sendFrameReadyEvents = true;
        player.frameReady += FrameDecoded;

        PortedManager.availabe.Add(this);
    }

    void FrameDecoded(VideoPlayer source, long frameIdx)
    {
        FrameBuffer.Push(frameIdx, player.texture);
        Debug.Log("Decoded " + frameIdx.ToString());
        PortedManager.availabe.Add(this);
        decoding = false;
    }

    void Update()
    {
        if (!decoding && PortedManager.toDecode.Count != 0)
        {
            frameIdx = PortedManager.toDecode.ElementAt(0);
            PortedManager.pending.Add(frameIdx);
            player.frame = frameIdx;
            Debug.Log("Decoding " + frameIdx.ToString());
            PortedManager.availabe.Remove(this);
            decoding = true;
        }
    }
}
