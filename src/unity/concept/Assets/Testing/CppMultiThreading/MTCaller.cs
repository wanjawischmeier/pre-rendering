using System;
using System.Collections;
using UnityEngine;
using PreRendering;
using Random = UnityEngine.Random;

public class MTCaller : MonoBehaviour
{
    public string videoPath;
    public int threads, iters, cacheSize, imgIdx;
    public Shader shader;

    private DecodingBuffer buffer;
    private Material material;

    private void Start()
    {
        Screen.SetResolution(1280, 720, false);
        Application.targetFrameRate = Screen.currentResolution.refreshRate;

        Decoder.Initialize(videoPath, threads, out IntPtr[] dataPointers);
        Decoder.FrameReady += OnFrameReady;
        Decoder.invokeFrameReadyEvents = true;

        buffer = new DecodingBuffer(dataPointers, Decoder.info, cacheSize, DecodingBuffer.BufferFormat.RGB24);

        material = new Material(shader);
        material.SetVector("Resolution", new Vector2(Decoder.info.width, Decoder.info.height));
        material.SetBuffer("InputBuffer", buffer.compute);
        material.SetInt("ImgIdx", -1);

        StartCoroutine(TestSeeks());
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
        for (int i = 0; i < iters; i++)
        {
            yield return new WaitForSeconds(1);

            // Can only be called on the main thread!
            int frame = Random.Range(0, (int)Decoder.info.frame_count - 1);
            Decoder.Decode(i, i);
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