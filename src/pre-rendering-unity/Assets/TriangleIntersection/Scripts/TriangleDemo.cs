using UnityEngine;
using ProjectionUtility;

[ExecuteInEditMode]
public class TriangleDemo : MonoBehaviour
{
    [Range(0, 3)]
    public float latitude;
    [Range(0, 360)]
    public float longitude;
    [Range(0, 1)]
    public float radius;

    public Transform CT, BT;
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
        Vector3 C = CT.position;
        Vector3 B = BT.position;

        if (drawMode == DrawMode.OneLine)
        {
            Vector2 latlon = new Vector2(latitude, longitude);

            TriangleCalculations.TranslateInsideSphere(latlon, C, B, radius, true);
        }
    }
}

namespace ProjectionUtility
{
    public static class TriangleCalculations
    {
        public static Vector2 TranslateInsideSphere(Vector2 latlon, Vector3 A, Vector3 B, float radius, bool drawLines = true, bool drawPoints = true, bool drawSpheres = true, float point_radius = 0.05f)
        {
            float b1 = Mathf.Sqrt(Mathf.Pow(B.x, 2) + Mathf.Pow(B.y, 2));
            float a1 = B.z;
            float b2 = radius;

            float c = Mathf.Sqrt(Mathf.Pow(b1, 2) + Mathf.Pow(a1, 2));

            // Atan2(b1, a1) instead?
            float alpha1 = Mathf.Atan(a1 / b1);
            float beta1 = 90 - alpha1;
            float beta3 = latlon.x;
            float beta2, alpha3;

            if (a1 < 0) beta2 = 180 - beta1 + beta3;
            else beta2 = 360 - beta1 - beta3;

            float gamma2 = Mathf.Asin(Mathf.Sin(beta2) / b2 * c);
            float alpha2 = 180 - beta2 - gamma2;

            if (a1 < 0) alpha3 = 90 - alpha1 - alpha2;
            else        alpha3 = 90 + alpha1 + alpha2;

            float a2 = b2 / Mathf.Sin(beta2) * Mathf.Sin(alpha2);

            Vector2 result = new Vector2(alpha3, latlon.y);

            if (drawLines)
            {
                Vector3 C1 = new Vector3(B.x, A.y, B.z);
                Debug.DrawLine(A, C1);
                Debug.DrawLine(B, C1);


                Debug.DrawLine(A, B);

                Vector3 C2 = result.ToVector(radius);
                Debug.DrawLine(A, C2);
                Debug.DrawLine(B, C2);
            }


            return result;
        }
    }
}