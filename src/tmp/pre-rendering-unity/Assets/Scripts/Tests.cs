using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using PreRendering;

public class Tests : MonoBehaviour
{
    public VideoClip video;
    public Vector2Int goalCoordinates;
    public Vector2Int lastGoalCoordinates;
    public string[] stringMonitor = new string[2];
    public int[] intMonitor = new int[10];
    Map2 map;
    PreRenderer preRenderer;
    void Start()
    {
        map = new Map2(video.originalPath);
        preRenderer = new PreRenderer(map);
    }

    // Update is called once per frame
    void Update()
    {
        //            Monitoring             //
        stringMonitor[0] = map.videoPath;
        stringMonitor[1] = map.filename;

        intMonitor[0] = map.fileData[0];
        intMonitor[1] = map.fileData[1];
        intMonitor[2] = map.fileData[2];
        intMonitor[3] = map.fileData[3];
        intMonitor[4] = map.fileData[4];
        intMonitor[5] = preRenderer.currentFrame;
        intMonitor[6] = preRenderer.currentCoordinates.x;
        intMonitor[7] = preRenderer.currentCoordinates.y;
        intMonitor[8] = preRenderer.currentDirection.x;
        intMonitor[9] = preRenderer.currentDirection.y;
        //____________________________________//

        if (goalCoordinates != lastGoalCoordinates)
        {
            preRenderer.currentCoordinates = goalCoordinates;
            lastGoalCoordinates = goalCoordinates;
        }
    }
}
