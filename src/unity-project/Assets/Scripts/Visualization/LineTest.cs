using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class LineTest : MonoBehaviour
{
    public Text text;
    Camera mainCam;
    Vector3 mouse;
    int frame = 0;

    void Start()
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        frame += 1;

        text.text = frame.ToString() + " frames";

        mouse = Input.mousePosition;
        mouse.z = 1;
    }

    private void OnDrawGizmos()
    {
        Vector3 start = new Vector3(Mathf.Sin(Time.realtimeSinceStartup * 0.2f) * 0.4f, -Mathf.Cos(Time.realtimeSinceStartup * 1.4f) * 0.2f);
        Vector3 end = new Vector3(-Mathf.Sin(Time.realtimeSinceStartup * 0.4f) * 0.6f, Mathf.Cos(Time.realtimeSinceStartup * 0.6f) * 0.4f);

        Gizmos.DrawLine(start, end);    // , new Color(start.x * 2, start.y * 2, end.x * 2), 4);
        Camera.main.backgroundColor = new Color(start.x * 2, start.y * 2, end.x * 2);
    }
}
