using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasTest : MonoBehaviour
{
    public RawImage image;

    CanvasRenderer canvasRenderer;

    void Start()
    {
        canvasRenderer = new CanvasRenderer(image, Color.clear);
    }

    void Update()
    {
        canvasRenderer.DrawLine(new Vector2(20, 20), new Vector2(40, 60), Color.blue);

        canvasRenderer.DrawRectangle(new Vector2Int(20, 20), new Vector2Int(140, 100), Color.green, Color.cyan);
    }
}
