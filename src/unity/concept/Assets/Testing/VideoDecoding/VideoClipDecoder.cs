using UnityEngine;
using UnityEngine.Video;
using PreRendering;
using System.Collections;

public class VideoClipDecoder : MonoBehaviour
{
    public string url;
    public MapConfig config;
    public int channelBlocks = 2;
    int chunkSize, totalSize;
    VideoPlayer[] players;
    public Texture2DArray chunk;

    private void Start()
    {
        chunkSize = Mathf.RoundToInt(Mathf.Pow(config.chunkWidth, 2));
        totalSize = chunkSize * config.chunkColumns * config.chunkRows;

        players = new VideoPlayer[channelBlocks];

        for (int i = 0; i < channelBlocks; i++)
        {
            players[i] = gameObject.AddComponent<VideoPlayer>();
            players[i].playOnAwake = false;
            players[i].waitForFirstFrame = false;
            players[i].skipOnDrop = false;
            players[i].sendFrameReadyEvents = true;
            players[i].source = VideoSource.Url;
            players[i].renderMode = VideoRenderMode.APIOnly;
            players[i].audioOutputMode = VideoAudioOutputMode.None;
            players[i].frameReady += Player_FrameReady;

            players[i].url = url;
            players[i].frame = i*totalSize;
            players[i].Prepare();
        }

        players[0].prepareCompleted += Player_PrepareCompleted;
    }

    private void Player_PrepareCompleted(VideoPlayer source)
    {
        chunk = new Texture2DArray((int)players[0].width, (int)players[0].height, chunkSize*2, TextureFormat.RGBA32, false);
        StartCoroutine(PlayAfterPreparation());
    }

    private void Player_FrameReady(VideoPlayer source, long frameIdx)
    {
        int index = (int)(frameIdx%chunkSize);
        // Graphics.CopyTexture(source.texture, 0, chunk, index);
    }

    private IEnumerator PlayAfterPreparation()
    {
        while (true)
        {
            bool done = true;

            foreach (var player in players)
                if (!player.isPrepared) done = false;

            if (done) break;

            yield return new WaitForSeconds(0.1f);
        }

        foreach (var player in players)
            player.Play();
    }

    private void Update()
    {
        
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(players[0].texture, destination);
    }
}
