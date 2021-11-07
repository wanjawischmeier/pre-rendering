using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class FPSCounter : MonoBehaviour
{
    private Text text;
    private const int Smoothing = 10;
    private int framesPassed = 0;
    private float fpsTotal = 0;

    private void Start() =>
        text = GetComponent<Text>();

    private void Update()
    {
        float fps = 1 / Time.unscaledDeltaTime;
        fpsTotal += fps;
        framesPassed++;

        text.text = $"FPS: {Mathf.Round((fpsTotal / framesPassed * 100) / 100)}";

        if (framesPassed > Smoothing)
        {
            framesPassed = 0;
            fpsTotal = 0;
        }
    }
}
