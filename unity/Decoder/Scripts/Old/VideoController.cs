using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoController : MonoBehaviour
{
    public bool setFrameOnChanged;
    public bool setFrameToRandom;
    public int framesRendered;
    public float timePerFrame;
    public VideoPlayer player;
    public Text text;
    Slider slider;
    ulong frameCount;

    void Start()
    {
        slider = GetComponent<Slider>();

        frameCount = player.clip.frameCount;

        slider.maxValue = frameCount;

        if (setFrameToRandom)
        {
            player.sendFrameReadyEvents = true;
            player.frameReady += ChangeFrameToRandom;
            ChangeFrameToRandom(player, 0);
        }
    }

    void Update()
    {

    }

    void ChangeFrameToRandom(VideoPlayer source, long frameIdx)
    {
        float start_time = Time.realtimeSinceStartup;
        source.frame = (long)Random.Range(0, frameCount);
        float time = Time.realtimeSinceStartup - start_time;
        Debug.Log("Frame set at: " + time.ToString());
        framesRendered += 1;
        text.text = string.Format("Frames rendered: {0}", framesRendered.ToString());
    }

    public void OnValueChanged()
    {
        if (setFrameOnChanged)
        {
            player.frame = (long)slider.value;
        }
    }
}
