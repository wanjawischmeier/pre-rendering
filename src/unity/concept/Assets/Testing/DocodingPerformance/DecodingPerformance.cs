using PreRendering;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class DecodingPerformance : MonoBehaviour
{
    public string imagePath;
    public long openCvTime = 0;
    public long wwwTime = 0;
    public Texture2D decodedLI;
    public Texture2D decodedIC;

    private void Start()
    {
        // Decoder.Initialize(imagePath, 1);
        // StartCoroutine(GetTexture());

        byte[] raw = File.ReadAllBytes(imagePath);

        decodedLI = new Texture2D(0, 0);
        decodedIC = new Texture2D(0, 0);
        
        decodedLI.LoadImage(raw);
        ImageConversion.LoadImage(decodedIC, raw);
    }
    /*
    private void Update()
    {
        var wwwWatch = new Stopwatch();

        Decoder.Decode(imagePath, 0, out openCvTime);
        
        wwwTime = wwwWatch.ElapsedMilliseconds;
    }
    */
    // Based on https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequestTexture.GetTexture.html
    private IEnumerator GetTexture()
    {
        var apiStopwatch = new Stopwatch();
        apiStopwatch.Start();
        
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(imagePath))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                // Get downloaded asset bundle
                var texture = DownloadHandlerTexture.GetContent(uwr);
            }
        }

        apiStopwatch.Stop();
        wwwTime = apiStopwatch.ElapsedMilliseconds;
    }
    /*
    private void OnDestroy()
    {
        Decoder.Deinitialize();
    }
    */
}
