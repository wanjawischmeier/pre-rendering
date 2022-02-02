using System.Collections;
using UnityEngine;
using PreRendering;

public class MTCaller : MonoBehaviour
{
    public string videoPath;
    public int threads, iters;

    ExternalVideoPlayer videoPlayer;

    private void Start()
    {
        Application.targetFrameRate = Screen.currentResolution.refreshRate;

        videoPlayer = new ExternalVideoPlayer(videoPath, threads);
        ExternalVideoPlayer.FrameReady += OnFrameReady;
        StartCoroutine(TestSeeks());
    }

    private void Update()
    {
        videoPlayer.computeBuffer.Refresh();
    }

    private void OnDestroy()
    {
        ExternalVideoPlayer.FrameReady -= OnFrameReady;
        videoPlayer.Release();
    }

    private IEnumerator TestSeeks()
    {
        yield return new WaitForSeconds(2);

        for (int i = 0; i < iters; i++)
        {
            // Can only be called on the main thread!
            int frame = Random.Range(0, (int)videoPlayer.info.frame_count - 1);
            videoPlayer.ReadToBuffer(i, 0, 0);
        }
    }

    private void OnFrameReady(long frameIdx, int threadIdx, int bufferIdx)
    {
        Debug.Log($"FrameReady callback for frame {frameIdx} from thread {threadIdx} invoked (stored at {bufferIdx})");
    }
}