using System;
using System.Collections;
using UnityEngine;
using PreRendering;

public class PreLoader : MonoBehaviour
{
    public string videoPath, imagePath;
    public MapConfig mapConfig;
    public int cacheSize, imgIdx;
    public Shader shader;
    public Vector2 res;
    public Vector2[] off;

    private DecodingBuffer buffer;
    private Decoder decoder;
    private Material material;

    private void Start()
    {
        Screen.SetResolution(1280, 720, false);
        Application.targetFrameRate = Screen.currentResolution.refreshRate;

        decoder = Decoder.Initialize(videoPath, 1, out IntPtr[] dataPointers)[0];
        Decoder.FrameReady += OnFrameReady;
        Decoder.invokeFrameReadyEvents = true;

        buffer = new DecodingBuffer(dataPointers, Decoder.info, DecodingBuffer.BufferFormat.RGB24);

        material = new Material(shader);
        material.SetVector("Resolution", Decoder.Resolution);
        // material.SetVector("Resolution", res);
        material.SetBuffer("InputBuffer", buffer.compute);
        material.SetInt("ImgIdx", -1);

        decoder.Decode(imagePath);

        /*
        off = new Vector2[cacheSize];

        StartCoroutine(TestSeeks());
        */
    }

    private void Update()
    {
        buffer.Refresh();
        material.SetInt("ImgIdx", imgIdx);
    }

    private void OnDestroy()
    {
        Decoder.FrameReady -= OnFrameReady;
        Decoder.Deinitialize();
        buffer.Release();
    }

    private IEnumerator TestSeeks()
    {
        ChunkIndexing.CalculateConstants(mapConfig, cacheSize, true);

        for (int i = 0; i < cacheSize; i++)
        {
            yield return new WaitForSeconds(1);

            Vector2 offset = ChunkIndexing.circularOffsets[i];
            long globalIndex = offset.ClampToChunkGrid().Grid.Global;
            off[i] = offset;
            
        }
    }

    private void OnFrameReady(long frameIdx, int threadIdx)
    {
        Debug.Log($"FrameReady callback for frame {frameIdx} from thread {threadIdx} invoked");
        buffer.Add(frameIdx, threadIdx);
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }
}