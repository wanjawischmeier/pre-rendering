using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FPSCounter : MonoBehaviour
{
    private TextMeshProUGUI text;
    private const float CounterRefreshFrequency = 1 / 15f;
    private int framesPassed = 0;
    private float lastUpdate = 0;
    private float fpsTotal = 0;

    private void Start() =>
        text = GetComponent<TextMeshProUGUI>();

    private void Update()
    {
        float fps = 1 / Time.unscaledDeltaTime;
        fpsTotal += fps;
        framesPassed++;

        if (Time.realtimeSinceStartup - lastUpdate > CounterRefreshFrequency)
        {
            text.text = $"FPS: {Mathf.Round(fpsTotal / framesPassed * 100 / 100)}\t({Mathf.Round(Time.unscaledDeltaTime * 100000) / 100}ms)";

            lastUpdate = Time.realtimeSinceStartup;
            framesPassed = 0;
            fpsTotal = 0;
        }
    }
}
