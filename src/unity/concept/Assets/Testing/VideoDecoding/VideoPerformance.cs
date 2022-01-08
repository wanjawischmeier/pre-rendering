using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Video;

public class VideoPerformance : MonoBehaviour
{
    [Header("Parameters")]
    public VideoClip clip;
    public int seeks = 250;
    public int frames = 500;

    [Header("Results")]
    public int seekTime;
    public int frameTime, chunkWidth;

    List<long> seekTimes, frameTimes;
    VideoPlayer player;
    Stopwatch stopwatch;

    void Start()
    {
        stopwatch = new Stopwatch();
        seekTimes = new List<long>();
        frameTimes = new List<long>();

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.waitForFirstFrame = false;
        player.skipOnDrop = false;
        player.sendFrameReadyEvents = true;
        player.source = VideoSource.VideoClip;
        player.renderMode = VideoRenderMode.APIOnly;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.playbackSpeed = 999;
        player.clip = clip;
        player.prepareCompleted += Player_PrepareCompleted;
        player.frameReady += Player_FrameReady;
        player.Prepare();
    }

    private void Player_PrepareCompleted(VideoPlayer source)
    {
        player.Play();
        player.frame = Random.Range(0, (int)player.frameCount - 1);
        stopwatch.Start();
    }

    private void Player_FrameReady(VideoPlayer source, long frameIdx)
    {
        stopwatch.Stop();
        if (seekTimes.Count < seeks)
        {
            seekTimes.Add(stopwatch.ElapsedMilliseconds);
            seekTime = seekTimes.Count;
            player.frame = Random.Range(0, (int)player.frameCount - 1);
        }
        else if (frameTimes.Count == 0 && seekTimes.Count == seeks && player.frame != 0)
        {
            player.frame = 0;
        }
        else if (frameTimes.Count < frames)
        {
            frameTimes.Add(stopwatch.ElapsedMilliseconds);
            frameTime = frameTimes.Count;
        }
        else
        {
            player.Stop();

            long t_seekTime = 0;
            foreach (long time in seekTimes)
                t_seekTime += time;
            seekTime = (int)(t_seekTime / seeks);

            long t_frameTime = 0;
            foreach (long time in frameTimes)
                t_frameTime += time;
            frameTime = (int)(t_frameTime / frames);
            chunkWidth = Mathf.RoundToInt(Mathf.Sqrt(seekTime / frameTime));

            return;
        }
        stopwatch.Restart();
    }
}
