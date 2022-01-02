using UnityEngine;
using UnityEngine.Video;
using PreRendering;
using static PreRendering.ChunkIndexing;
using System.Collections.Generic;

public class VideoClipDecoder : MonoBehaviour
{
    [Header("Source")]
    public string url;
    public MapConfig config;
    public Shader shader;

    [Header("Player")]
    public Vector2 position;

    [Header("Performance")]
    public int targetFps = 60;
    public int tolerance = 10;
    public int intervallSize = 100;
    public float rateOfChange = 0.1f;
    public int searchCircleRadius = 3;
    public bool loaded = false;

    ComputeBuffer buffer;
    Texture2DArray chunk;
    Material material;
    VideoPlayer[] players;
    int[] chunkIndicies;
    int prepared = 0;
    long lastPlaybackSpeedChange = 0;

    private void Start()
    {
        nclip = config.nclip;
        fclip = config.fclip;
        blockWidth = config.blockWidth;
        blockHeight = config.blockHeight;
        chunkWidth = config.chunkWidth;
        chunkColumns = config.chunkColumns;
        chunkRows = config.chunkRows;
        channelBlocks = config.channelBlocks;
        circleRadius = searchCircleRadius;
        CalculateConstants();

        players = new VideoPlayer[channelBlocks];
        chunkIndicies = new int[chunkSize * channelBlocks];
        buffer = new ComputeBuffer(chunkIndicies.Length, sizeof(int));
        material = new Material(shader);
        
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
            player.Prepare();

            players[i] = player;
        }
    }

    private void Update()
    {
        float fps = 1 / Time.unscaledDeltaTime;
        material.SetVector("Position", position);

        // Optimize playback speed
        if (Time.frameCount > lastPlaybackSpeedChange + intervallSize)
        {
            foreach (var player in players)
            {
                float factor = 0;

                if (fps < targetFps - tolerance)
                    factor = -rateOfChange;
                else if (fps > targetFps + tolerance)
                    factor = rateOfChange;

                if (factor != 0 && player.isPlaying)
                {
                    player.playbackSpeed = Mathf.Max(player.playbackSpeed + factor, rateOfChange);
                    lastPlaybackSpeedChange = Time.frameCount;
                }
            }
        }

        loaded = true;

        // Restart any paused players if needed
        for (int i = 0; i < config.channelBlocks; i++)
        {
            VideoPlayer player = players[i];

            if (player.isPaused)
            {
                var globalIndex = player.frame.GetGlobalIndex(out int channelBlock);
                CorrectChunkIndex(globalIndex, position, out ChunkIndex newChunkIndex, out GlobalIndex newFrame);

                // Check wether chunk is fully loaded
                if (chunkIndicies[0] != newChunkIndex)
                {
                    if (player.frame != newFrame)
                        player.frame = newFrame;

                    player.Play();
                }
            }
            else loaded = false;
        }
    }

    private void OnDisable()
    {
        buffer.Release();
    }

    private void Player_PrepareCompleted(VideoPlayer source)
    {
        prepared += 1;

        // Check if all players are prepared
        if (prepared == config.channelBlocks)
        {
            chunk = new Texture2DArray((int)source.width, (int)source.height, chunkIndicies.Length, TextureFormat.RGBA32, false);
            material.SetTexture("_MainTex", chunk);

            foreach (var player in players)
                player.Play();
        }
    }

    private void Player_FrameReady(VideoPlayer source, long frameIdx)
    {
        var globalIndex = frameIdx.GetGlobalIndex(out int channelBlock);
        int localIndex = globalIndex.Local;
        bool correct = CorrectChunkIndex(globalIndex, position, out ChunkIndex newChunkIndex, out GlobalIndex newFrame);
        
        Graphics.CopyTexture(source.texture, 0, chunk, localIndex);
        chunkIndicies[localIndex] = newChunkIndex;
        buffer.SetData(chunkIndicies, localIndex, localIndex, 1);

        // Finished decoding channel block of chunk
        if ((frameIdx + 1) % chunkSize == 0 && (frameIdx + 1) - channelBlock * totalSize != 0)
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

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int available = 0;

        foreach (var offset in circularOffsets)
        {
            var clamped = (position + offset).ClampToChunkGrid();
            var chunkIndex = clamped.Chunk.Global;
            var localIndex = clamped.Grid.Local.Local;

            // The point is in the buffer
            if (chunkIndicies[localIndex] == chunkIndex)
            {
                available = localIndex;
                break;
            }
        }

        material.SetInteger("Index", available);
        Graphics.Blit(players[0].texture, destination, material);
    }
}
