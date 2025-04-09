using UnityEngine;

[ExecuteInEditMode]
public class DataPointManager : MonoBehaviour
{
    public Material material;
    public Vector4[] dataPoints;

    private void Update()
    {
        // Update the material with the data points
        if (material != null && dataPoints != null && dataPoints.Length > 0)
        {
            material.SetVectorArray("_DataPoints", dataPoints);
        }
    }
}
