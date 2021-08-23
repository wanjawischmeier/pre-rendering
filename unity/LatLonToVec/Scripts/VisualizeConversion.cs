using UnityEngine;
using ProjectionUtility;
using ColorMode = ProjectionUtility.SphereCalculations.ColorMode;

[ExecuteInEditMode]
public class VisualizeConversion : MonoBehaviour
{
    [Range(0, 1)]
    public float latitude;
    [Range(0, 1)]
    public float longitude;
    [Range(0, 1)]
    public float radius;
    [Range(0, 2)]
    public float radius2;

    public Vector2Int steps;
    public DrawMode drawMode;

    public enum DrawMode
    {
        OneLine,
        Multiple
    }

    void Start()
    {
        
    }

    private void OnDrawGizmos()
    {
        Vector3 P1 = transform.position;

        // LatLon to Vector2
        Vector2 latlon = new Vector2(latitude, longitude);

        if (drawMode == DrawMode.OneLine)
        {
            SphereCalculations.TranslateSimple(latlon, P1, radius, ColorMode.defaultMode);
        }
        else if (drawMode == DrawMode.Multiple)
        {
            SphereCalculations.TranslateSimple(latlon, P1, radius, ColorMode.defaultMode);

            for (float lat = 0; lat < steps.x; lat += Mathf.PI * 2 / steps.x)
            {
                for (float lon = 0; lon < steps.y; lon += Mathf.PI / steps.y)
                {
                    ColorMode mode1 = ColorMode.defaultMode;
                    ColorMode mode2 = ColorMode.defaultMode;

                    mode2.P3 = Color.black;
                    mode2.P4 = Color.black;

                    SphereCalculations.TranslateSimple(new Vector2(lat, lon), P1, radius, mode1);
                    SphereCalculations.TranslateSimple(new Vector2(lat, lon), P1, radius, mode2);
                }
            }
        }
    }
}