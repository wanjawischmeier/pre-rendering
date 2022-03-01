using System;
using UnityEngine;
using UnityEngine.Video;
using PreRendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(ShaderManager))]
[RequireComponent(typeof(MovementController))]
public class VideoClipDecoder : MonoBehaviour
{
    #region Headers

    [Header("Source")]
    public string relativeVideoPath;
    public MapConfig config;

    [Header("Performance")]
    public int searchCircleRadius = 3;
    public bool loaded = false;
    
    [Header("Decoder")]
    public float predictionBlend = 0.75f;
    public float predictionDistance = 2;
    public int cacheSize = 10;
    public int decodingThreads = 4;
    public int maxPending = 8;

    #endregion

    #region Data

    [NonSerialized]
    public ShaderManager shaderManager;
    [NonSerialized]
    public DecodingManager decodingManager;

    private Resolution projectionResolution;
    private Resolution screenResolution;
    private Vector3 positionOffset = default;
    private Vector3 lastPosition = default;
    // private MovementController controller;
    private long lastPlaybackSpeedChange = 0;
    private int prepared = 0;

    #endregion

    private void Awake()
    {
        shaderManager = GetComponent<ShaderManager>();
        // controller = GetComponent<MovementController>();

        ChunkIndexing.CalculateConstants(config, searchCircleRadius);

        // decodingManager = new DecodingManager(relativeVideoPath, decodingThreads, maxPending);
        // Decoder.FrameReady += OnFrameReady;
    }

    private void Update()
    {
        float fps = 1 / Time.unscaledDeltaTime;

        loaded = true;
        /*
        // Restart any paused players if needed
        for (int i = 0; i < config.channelBlocks; i++)
        {
            if (player.isPaused)
            {
                var globalIndex = player.frame.GetGlobalIndex(out int channelBlock);
                ChunkIndexing.CorrectChunkIndex(globalIndex, transform.position, out ChunkIndexing.ChunkIndex newChunkIndex, out ChunkIndexing.GlobalIndex newFrame);

                // Check wether chunk is fully loaded
                if (ChunkIndexing.chunkIndicies[0] != newChunkIndex)
                {
                    if (player.frame != newFrame)
                        player.frame = newFrame;

                    player.Play();
                }
            }
            else loaded = false;
        }
        */
    }

    private void OnDisable() => decodingManager?.Release();

    private void OnFrameReady(long frameIdx, int threadIdx)
    {
        var globalIndex = frameIdx.GetGlobalIndex(out int channelBlock);
        int localIndex = globalIndex.Local;
        bool correct = ChunkIndexing.CorrectChunkIndex(globalIndex, transform.position, out ChunkIndexing.ChunkIndex newChunkIndex, out ChunkIndexing.GlobalIndex newFrame);
        /*
        Graphics.CopyTexture(source.texture, 0, chunk, localIndex);
        ChunkIndexing.chunkIndicies[localIndex] = newChunkIndex;
        buffer.SetData(ChunkIndexing.chunkIndicies, localIndex, localIndex, 1);

        // Finished decoding channel block of chunk
        if ((frameIdx + 1) % ChunkIndexing.chunkSize == 0 && (frameIdx + 1) - channelBlock * ChunkIndexing.totalSize != 0)
        {
            ChunkIndexing.chunkIndicies[localIndex] = newChunkIndex;
            buffer.SetData(ChunkIndexing.chunkIndicies, localIndex, localIndex, 1);

            Debug.LogFormat("Finished decoding channel block {0} of chunk {1}.", channelBlock, (int)newChunkIndex);
            source.Pause();
            return;
        }

        // Player is loading the wrong chunk
        if (!correct)
            source.frame = newFrame;
        */
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        int available = 0;

        foreach (var offset in ChunkIndexing.circularOffsets)
        {
            var clamped = (transform.position + offset.Expand()).ClampToChunkGrid();
            var chunkIndex = clamped.Chunk.Global;
            var localIndex = clamped.Grid.Local.Local;

            // The point is in the buffer
            if (ChunkIndexing.chunkIndicies[localIndex] == chunkIndex)
            {
                available = localIndex;
                break;
            }
        }

        shaderManager.BlitWithIndex(available, ref destination);
    }
}
