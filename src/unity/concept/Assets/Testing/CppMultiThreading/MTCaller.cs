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
    public MapConfig config;
    public int searchCircleRadius = 3;

    private DecodingManager manager;
    private Material material;

    private void Start()
    {
        Screen.SetResolution(1280, 720, false);
        Application.targetFrameRate = Screen.currentResolution.refreshRate;

        ChunkIndexing.CalculateConstants(config, searchCircleRadius);
        manager = new DecodingManager(videoPath, threads);
        
        material = new Material(shader);
        material.SetVector("Resolution", new Vector2(Decoder.info.width, Decoder.info.height));
        material.SetBuffer("InputBuffer", manager.buffer.compute);
        material.SetInt("ImgIdx", -1);

        StartCoroutine(TestSeeks());
    }

    private void Update()
    {
        manager.Refresh();
        material.SetInt("ImgIdx", imgIdx);
    }

    private void OnDestroy()
    {
        manager.Release();
    }

    private IEnumerator TestSeeks()
    {
        for (int i = 0; i < iters; i++)
        {
            yield return new WaitForSeconds(1);

            // Can only be called on the main thread!
            int frame = Random.Range(0, (int)Decoder.info.frame_count - 1);
            manager.Decode(frame, i);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }
}