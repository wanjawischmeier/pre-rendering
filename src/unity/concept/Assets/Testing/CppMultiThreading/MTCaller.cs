using System.Collections;
using UnityEngine;
using PreRendering;

public class MTCaller : MonoBehaviour
{

    public string videoPath;
    public int threads, iters, cacheSize, imgIdx;
    public Shader shader;

    private Material material;

    private void Start()
    {
        Screen.SetResolution(1280, 720, true);
        Application.targetFrameRate = Screen.currentResolution.refreshRate;
        
        ExternalVideoPlayer.Initialize(videoPath, threads, cacheSize);
        ExternalVideoPlayer.FrameReady += OnFrameReady;
        ExternalVideoPlayer.invokeFrameReadyEvents = true;

        material = new Material(shader);
        material.SetVector("Resolution", new Vector2(ExternalVideoPlayer.info.width, ExternalVideoPlayer.info.height));
        material.SetBuffer("InputBuffer", ExternalVideoPlayer.buffer.computeBuffer);

        StartCoroutine(TestSeeks());
    }

    private void Update()
    {
        ExternalVideoPlayer.buffer.Refresh();
        material.SetInt("ImgIdx", imgIdx);
    }

    private void OnDestroy()
    {
        ExternalVideoPlayer.FrameReady -= OnFrameReady;
        ExternalVideoPlayer.Release();
    }

    private IEnumerator TestSeeks()
    {
        for (int i = 0; i < iters; i++)
        {
            yield return new WaitForSeconds(1);

            // Can only be called on the main thread!
            int frame = Random.Range(0, (int)ExternalVideoPlayer.info.frame_count - 1);
            ExternalVideoPlayer.ReadToBuffer(i, 0, 0);
        }
    }

    private void OnFrameReady(long frameIdx, int threadIdx, int bufferIdx)
    {
        Debug.Log($"FrameReady callback for frame {frameIdx} from thread {threadIdx} invoked (stored at {bufferIdx})");
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(null, destination, material);
    }
}