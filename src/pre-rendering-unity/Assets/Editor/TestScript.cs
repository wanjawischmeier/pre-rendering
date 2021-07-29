using UnityEditor;
using UnityEngine;

public class TestScript
{
    [MenuItem("Assets/Maps/Test")]
    static void InspectMapBundle()
    {
        Debug.Log(Application.dataPath);
    }
}
