using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class FPS : MonoBehaviour
{
    Text text;
    const int smoothing = 10;
    int framesPassed = 0;
    float fpsTotal = 0;

    void Start() =>
        text = GetComponent<Text>();

    void Update()
    {
        float fps = 1 / Time.unscaledDeltaTime;
        fpsTotal += fps;
        framesPassed++;
        text.text = string.Format("FPS: {0}", Mathf.Round((fpsTotal / framesPassed * 100) / 100));

        if (framesPassed > smoothing)
        {
            framesPassed = 0;
            fpsTotal = 0;
        }
    }
}
