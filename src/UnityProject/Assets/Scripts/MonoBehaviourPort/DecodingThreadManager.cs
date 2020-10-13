using PreRendering;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

public class DecodingThreadManager : MonoBehaviour
{
    VideoPlayer player;
    bool decoding = false;
    long targetFrameIdx;
    public int decodingTime = 0;

    void Start()
    {
        player = GetComponent<VideoPlayer>();
        // player.sendFrameReadyEvents = true;
        // player.frameReady += FrameDecoded;

        PortedManager.availabe.Add(this);
    }

    void FrameDecoded(VideoPlayer source, long frameIdx)
    {
        FrameBuffer.Push(frameIdx, player.texture);
        PortedManager.pending.Remove(frameIdx);
        PortedManager.availabe.Add(this);
        decoding = false;
        decodingTime = Time.frameCount - decodingTime;
    }

    private void Update()
    {
        if (player.frame == targetFrameIdx && decoding)
        {
            FrameDecoded(player, targetFrameIdx);
        }
    }

    public void Decode(long frameIdx)
    {
        // frameIdx = PortedManager.toDecode.ElementAt(0);
        decodingTime = Time.frameCount;
        targetFrameIdx = frameIdx;
        PortedManager.pending.Add(frameIdx);
        player.frame = frameIdx;
        PortedManager.availabe.Remove(this);
        decoding = true;
    }
}
