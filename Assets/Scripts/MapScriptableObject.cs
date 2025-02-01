using UnityEngine;

[CreateAssetMenu(fileName = "MapScriptableObject", menuName = "ScriptableObjects/MapScriptableObject")]
public class MapScriptableObject : ScriptableObject
{
    public float nearClipPlane, farClipPlane, flySpeedMultiplier = 1;
    public Texture2D[] inputImages;
    public Vector4[] cubemapPositions;
}
