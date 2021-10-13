using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PreRendering;

public class Testing : MonoBehaviour
{
    // public string path;
    /*
    [System.Serializable]
    public struct KeyVal
    {
        public Vector3 key;
        public int val;
    }
    public Texture2D[] textureArray;
    public Texture2DArray textures;
    public KeyVal[] reserved;
    public int[] available;
    */
    void Start()
    {
        // ---- Map Test
        /*
        Map map = new Map(path);
        Debug.Log(map.fclip);
        Debug.Log(map.mxWidth);
        Debug.Log(map.textureWidth);
        Debug.Log(map.textureHeight);
        Debug.Log(map.vectorOffsets.Length);
        */
        // ---- Buffer Test
        /*
        FrameBuffer buffer = new FrameBuffer(1024, 512, 3);

        for (int i = 0; i < textureArray.Length; i++)
        {
            buffer.Add(new Vector3(0, 0, i), textureArray[i]);
        }

        int idx;
        reserved = new KeyVal[buffer.reserved.Count];
        available = new int[buffer.available.Count];

        idx = 0;
        foreach (KeyValuePair<Vector3, int> item in buffer.reserved)
        {
            KeyVal keyVal = new KeyVal { key = item.Key, val = item.Value };
            reserved[idx++] = keyVal;
        }

        idx = 0;
        foreach (int item in buffer.available)
            available[idx++] = item;

        textures = buffer.textures;
        */
    }
}
