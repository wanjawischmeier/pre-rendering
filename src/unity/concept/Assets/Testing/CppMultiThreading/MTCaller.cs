using System.Collections;
using UnityEngine;
using PreRendering;

public class MTCaller : MonoBehaviour
{
    public string videoPath;
    public int threads, iters, cacheSize, address, comp;
    public Vector2 res;
    public Texture2D texture;
    public byte[] array;
    public Shader shader;

    private Material material;

    private void Start()
    {
        Screen.SetResolution(1280, 720, true);
        Application.targetFrameRate = Screen.currentResolution.refreshRate;
        
        ExternalVideoPlayer.Initialize(videoPath, threads, cacheSize);
        ExternalVideoPlayer.FrameReady += OnFrameReady;
        ExternalVideoPlayer.invokeFrameReadyEvents = true;

        int size = ExternalVideoPlayer.ImageSize * cacheSize;
        array = new byte[size];
        for (int y = 0; y < ExternalVideoPlayer.info.height; y++)
        {
            for (int x = 0; x < ExternalVideoPlayer.info.width; x++)
            {
                for (int i = 0; i < 3; i++)
                {
                    int idx = (x + y * ExternalVideoPlayer.info.width) * 3 + i;
                    // int idx = Mathf.RoundToInt((x + (res.y - 1 - y) * res.x) * 2);
                    array[idx] = (byte)Mathf.Min(x * 2 + y, 0xFF);
                }
            }
        }
        ExternalVideoPlayer.buffer.computeBuffer.SetData(array);

        texture = new Texture2D(ExternalVideoPlayer.info.width, ExternalVideoPlayer.info.height);
        for (int y = 0; y < ExternalVideoPlayer.info.height; y++)
        {
            for (int x = 0; x < ExternalVideoPlayer.info.width; x++)
            {
                int idx = Mathf.FloorToInt(x + y * ExternalVideoPlayer.info.width) * 3;
                // int idx = Mathf.RoundToInt((x + (res.y - 1 - y) * res.x) * 2);
                byte r = array[idx];
                byte g = array[idx +1];
                byte b = array[idx +2];

                texture.SetPixel(x, y, new Color32(r, g, b, 0xFF));
            }
        }
        texture.Apply();
        ExternalVideoPlayer.buffer.computeBuffer.SetData(array);


        material = new Material(shader);
        // material.SetVector("Resolution", new Vector2(ExternalVideoPlayer.info.width, ExternalVideoPlayer.info.height));
        material.SetBuffer("InputBuffer", ExternalVideoPlayer.buffer.computeBuffer);
        material.SetVector("Resolution", res);

        StartCoroutine(TestSeeks());
    }

    private void Update()
    {
        // ExternalVideoPlayer.buffer.Refresh();
        // ExternalVideoPlayer.buffer.computeBuffer.GetData(array);
        material.SetVector("Resolution", res);
        material.SetInt("Address", address);
        material.SetInt("Comp", comp);
    }

    private void OnDestroy()
    {
        ExternalVideoPlayer.FrameReady -= OnFrameReady;
        ExternalVideoPlayer.Release();
    }

    private IEnumerator TestSeeks()
    {
        yield return new WaitForSeconds(2);

        for (int i = 0; i < iters; i++)
        {
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
        Graphics.Blit(source, destination, material);
    }
}