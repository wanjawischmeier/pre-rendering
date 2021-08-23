using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineTesting2 : MonoBehaviour
{
    public Material material;
    ShapeRenderer lineRenderer;
    ShapeRenderer rect;
    
    void Start()
    {
        lineRenderer = new ShapeRenderer(Camera.main, material, 100);
        rect = new ShapeRenderer(Camera.main, material, 100);
    }

    void Update()
    {
        Vector3 start = new Vector3(Mathf.Sin(Time.realtimeSinceStartup * 0.2f) * 0.4f, -Mathf.Cos(Time.realtimeSinceStartup * 1.4f) * 0.2f);
        Vector3 end = new Vector3(-Mathf.Sin(Time.realtimeSinceStartup * 0.4f) * 0.6f, Mathf.Cos(Time.realtimeSinceStartup * 0.6f) * 0.4f);

        lineRenderer.DrawLine(start, end, new Color(start.x * 2, start.y * 2, end.x * 2), 0.001f);
        rect.DrawRectangle(new Vector3(start.x, start.y), new Vector2(0.05f, 0.05f), Color.cyan, 0.005f, 1);
        rect.DrawRectangle(new Vector3(end.x, end.y), new Vector2(0.05f, 0.05f), Color.blue, 0.005f, 1);
    }
}
