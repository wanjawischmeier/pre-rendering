using UnityEngine;
using UnityEngine.Video;
using PreRendering;
using System;

public class VideoClipDecoder : MonoBehaviour
{
    public string url;
    public MapConfig config;
    public Vector2 position;
    VideoPlayer[] players;
    public int[] chunkIndicies;
    ComputeBuffer buffer;
    int chunkSize, totalSize;
    int prepared = 0;
    public Texture2DArray chunk;

    enum VideoPlayerState
    {
        Idle,
        Decoding
    }

    private void Start()
    {
        chunkSize = Mathf.RoundToInt(Mathf.Pow(config.chunkWidth, 2));
        totalSize = chunkSize * config.chunkColumns * config.chunkRows;

        players = new VideoPlayer[config.channelBlocks];
        chunkIndicies = new int[chunkSize * config.channelBlocks];
        buffer = new ComputeBuffer(chunkIndicies.Length, sizeof(int));
        
        for (int i = 0; i < config.channelBlocks; i++)
        {
            VideoPlayer player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.waitForFirstFrame = false;
            player.skipOnDrop = false;
            player.sendFrameReadyEvents = true;
            player.source = VideoSource.Url;
            player.renderMode = VideoRenderMode.APIOnly;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.prepareCompleted += Player_PrepareCompleted;
            player.frameReady += Player_FrameReady;

            player.url = url;
            player.frame = i * totalSize;
            player.playbackSpeed = 1;
            player.Prepare();

            players[i] = player;
        }
    }

    private void Player_PrepareCompleted(VideoPlayer source)
    {
        prepared += 1;

        // Check if all players are prepared
        if (prepared == config.channelBlocks)
        {
            chunk = new Texture2DArray((int)source.width, (int)source.height, chunkIndicies.Length, TextureFormat.RGBA32, false);

            foreach (var player in players)
                player.Play();
        }
    }

    private void Player_FrameReady(VideoPlayer source, long frameIdx)
    {
        long frame = frameIdx % totalSize;
        int channelBlock = Array.IndexOf(players, source);
        int localIndex = (int)(frame % chunkSize) + channelBlock * chunkSize;
        bool correct = CorrectChunkIndex(source, channelBlock, frame, position, out int newChunkIndex, out long newFrame);

        Graphics.CopyTexture(source.texture, 0, chunk, localIndex);
        chunkIndicies[localIndex] = newChunkIndex;
        buffer.SetData(chunkIndicies, localIndex, localIndex, 1);

        // Finished decoding channel block of chunk
        if ((frameIdx +1) % chunkSize == 0 && (frameIdx +1) - channelBlock * totalSize != 0)
        {
            chunkIndicies[localIndex] = newChunkIndex;
            buffer.SetData(chunkIndicies, localIndex, localIndex, 1);

            Debug.LogFormat("Finished decoding channel block {0} of the chunk.", channelBlock);
            source.Pause();
            return;
        }

        // Player is loading the wrong chunk
        if (!correct)
            source.frame = newFrame;
    }

    private void Update()
    {
        for (int i = 0; i < config.channelBlocks; i++)
        {
            VideoPlayer player = players[i];
            int channelBlock = Array.IndexOf(players, player);

            if (player.isPaused)
            {
                int start = channelBlock * chunkSize;
                int end = start + chunkSize;

                CorrectChunkIndex(player, channelBlock, player.frame, position, out int newChunkIndex, out long newFrame);

                // Check wether chunk is fully loaded
                if (chunkIndicies[0] != newChunkIndex)
                {
                    if (player.frame != newFrame)
                        player.frame = newFrame;

                    player.Play();
                }
            }
        }
    }

    private void OnDisable()
    {
        buffer.Release();
    }

    private bool CorrectChunkIndex(VideoPlayer player, int channelBlock, long frame, Vector2 position, out int newChunkIndex, out long newFrame)
    {
        int currentChunkIndex = Mathf.FloorToInt(frame / chunkSize);

        Vector2 clamped = new Vector2(
            Mathf.Clamp(position.x, 0, config.chunkWidth * config.chunkColumns - 1),
            Mathf.Clamp(position.y, 0, config.chunkWidth * config.chunkRows - 1));

        Vector2Int chunkPosition = new Vector2Int(
            Mathf.FloorToInt(clamped.x / config.chunkWidth),
            Mathf.FloorToInt(clamped.y / config.chunkWidth));

        newChunkIndex = chunkPosition.x + chunkPosition.y * config.chunkColumns;
        newFrame = newChunkIndex * chunkSize + channelBlock * totalSize;

        return currentChunkIndex == newChunkIndex;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(players[0].texture, destination);
    }
}
