using UnityEngine;
using UnityEngine.Video;
using PreRendering;
using static PreRendering.ChunkIndexing;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
[RequireComponent(typeof (MovementController))]
public class VideoClipDecoder : MonoBehaviour
{
    [Header("Source")]
    public string url;
    public MapConfig config;
    public Shader shader;

    [Header("Performance")]
    public int targetFps = 60;
    public int tolerance = 10;
    public int intervallSize = 100;
    public float rateOfChange = 0.1f;
    public float pIcon = 0.05f;
    public int searchCircleRadius = 3;
    public bool loaded = false;
    
    [Header("Map")]
    public string renderPath;
    public string[] mapPaths;
    public string[] mapFiles;
    public int mapSelection;
    private string mapPath;

    [Header("Decoder")]
    public float predictionBlend = 0.75f;
    public float predictionDistance = 2;
    public int cacheSize = 10;
    public int decodingThreads = 4;

    [Header("Projection & Post Processing")]
    public float geometryPercision = 0.75f;
    public ShaderDebugMode shaderDebug = ShaderDebugMode.Disabled;
    public float depthOfField = 0;
    public float mistOffset = 1;
    public float mistFalloff = 0.1f;
    public Color mistColor = Color.white;

    private Resolution projectionResolution;
    private Resolution screenResolution;
    private Vector3 positionOffset = default;
    private Vector3 lastPosition = default;
    private MovementController controller;
    private Texture2DArray chunk;
    private ComputeBuffer buffer;
    private VideoPlayer[] players;
    private Material material;
    private Camera mainCamera;
    private long lastPlaybackSpeedChange = 0;
    private int[] chunkIndicies;
    private int prepared = 0;

    public enum ShaderDebugMode
    {
        Disabled,
        TextureCoordinates,
        ProjectedCoordinates,
        Normals,
        DepthOfField,
        DepthBuffer
    }

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

        mainCamera = GetComponent<Camera>();
        players = new VideoPlayer[channelBlocks];
        chunkIndicies = new int[chunkSize * channelBlocks];
        buffer = new ComputeBuffer(chunkIndicies.Length, sizeof(int));
        material = new Material(shader);
        material.SetInt("MX_IDX", cacheSize);
        material.SetFloat("PI", Mathf.PI);
        material.SetFloat("PI2", Mathf.PI * 2);
        material.SetFloat("NegativeInfinity", float.NegativeInfinity);
        material.SetFloat("PositiveInfinity", float.PositiveInfinity);
        material.SetFloat("NCLIP", nclip);
        material.SetFloat("FCLIP", fclip);
        material.SetBuffer("ChunkIndicies", buffer);

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

        material.SetInt("DEBUG", (int)shaderDebug);
        material.SetFloat("FOV", mainCamera.fieldOfView * Mathf.Deg2Rad);
        material.SetFloat("PLAYER_ICON", pIcon);
        material.SetFloat("DOF_INTENSITY", depthOfField);
        material.SetFloat("MIST_FALLOFF", mistFalloff);
        material.SetFloat("MIST_OFFSET", mistOffset);
        material.SetVector("MIST_COLOR", mistColor);
        material.SetVector("POSITION", transform.position);
        material.SetVector("ROTATION", transform.eulerAngles * Mathf.Deg2Rad);

        loaded = true;

        // Restart any paused players if needed
        for (int i = 0; i < config.channelBlocks; i++)
        {
            VideoPlayer player = players[i];

            if (player.isPaused)
            {
                var globalIndex = player.frame.GetGlobalIndex(out int channelBlock);
                CorrectChunkIndex(globalIndex, transform.position, out ChunkIndex newChunkIndex, out GlobalIndex newFrame);

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
            material.SetTexture("_InputBuffer", chunk);

            foreach (var player in players)
                player.Play();
        }
    }

    private void Player_FrameReady(VideoPlayer source, long frameIdx)
    {
        var globalIndex = frameIdx.GetGlobalIndex(out int channelBlock);
        int localIndex = globalIndex.Local;
        bool correct = CorrectChunkIndex(globalIndex, transform.position, out ChunkIndex newChunkIndex, out GlobalIndex newFrame);
        
        Graphics.CopyTexture(source.texture, 0, chunk, localIndex);
        chunkIndicies[localIndex] = newChunkIndex;
        buffer.SetData(chunkIndicies, localIndex, localIndex, 1);

        // Finished decoding channel block of chunk
        if ((frameIdx + 1) % chunkSize == 0 && (frameIdx + 1) - channelBlock * totalSize != 0)
        {
            chunkIndicies[localIndex] = newChunkIndex;
            buffer.SetData(chunkIndicies, localIndex, localIndex, 1);

            Debug.LogFormat("Finished decoding channel block {0} of chunk {1}.", channelBlock, (int)newChunkIndex);
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
            var clamped = (transform.position + offset.Expand()).ClampToChunkGrid();
            var chunkIndex = clamped.Chunk.Global;
            var localIndex = clamped.Grid.Local.Local;

            // The point is in the buffer
            if (chunkIndicies[localIndex] == chunkIndex)
            {
                available = localIndex;
                break;
            }
        }

        material.SetInt("IMG_IDX", available);
        Graphics.Blit(null, destination, material);
        // Graphics.Blit(chunk, destination, available, 0);
    }
}
