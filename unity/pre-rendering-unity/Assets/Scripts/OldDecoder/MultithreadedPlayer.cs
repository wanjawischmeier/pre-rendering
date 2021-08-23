using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.Video;
using Buffering;
using Random = System.Random;

public class MultithreadedPlayer : MonoBehaviour
{
    [System.Serializable]
    public class Player
    {
        public GameObject gameObject;
        public VideoPlayer videoPlayer;
        public ulong frameCount;
        Func<VideoPlayer, long, bool> listener;

        public Player(GameObject preFab, Transform parent, int thread, Func<VideoPlayer, long, bool> _listener)
        {
            gameObject = Instantiate(preFab);
            gameObject.name = "Player " + thread.ToString();
            gameObject.transform.SetParent(parent);
            videoPlayer = gameObject.GetComponent<VideoPlayer>();
            videoPlayer.sendFrameReadyEvents = true;
            videoPlayer.frameReady += Run;
            frameCount = videoPlayer.frameCount;
            listener = _listener;
        }

        void Run(VideoPlayer source, long frame)
        {
            listener(source, frame);
        }
    }

    

    [Range(1, 10)]
    public int threads;
    [Range(1, 20)]
    public ulong bufferSize;
    
    public GameObject playerObject;
    Buffer<string> buffer;
    public Player[] players;
    public FrameBufferV1 frameBuffer;

    void Start()
    {
        players = new Player[threads];
        // buffer = new Buffer<string>(bufferSize, "tst");
        frameBuffer = new FrameBufferV1(bufferSize, 100, 100, Decode);
        // Random random = new Random();
        /*
        for (int i = 0; i < threads; i++)
        {
            players[i] = new Player(playerObject, transform, i, FrameReady);
        }
        
        // buffer.Log();
        for (ulong i = 0; i < 20; i++)
        {
            buffer.Push((ulong)random.Next(0, 10), "Test" + i.ToString());
        }

        // buffer.Log();
        */
    }

    Texture Decode(ulong frameIdx)
    {
        return new Texture2D(100, 100);
    }

    bool FrameReady(VideoPlayer source, long frame)
    {
        Debug.Log("Loaded");
        return true;
    }
}
