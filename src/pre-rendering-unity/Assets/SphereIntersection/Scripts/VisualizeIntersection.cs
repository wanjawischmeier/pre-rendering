using UnityEngine;
using ProjectionUtility;
using ColorMode = ProjectionUtility.SphereCalculations.ColorMode;

[ExecuteInEditMode]
public class VisualizeIntersection : MonoBehaviour
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
    public Transform CT;
    public Transform P1T;

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
        Vector3 P1 = P1T.position;

        // LatLon to Vector2
        Vector2 latlon = new Vector2(latitude, longitude);

        if (drawMode == DrawMode.OneLine)
        {
            SphereCalculations.TranslateInsideSphere(latlon, C, P1, radius, radius2, ColorMode.defaultMode);
        }
        else if (drawMode == DrawMode.Multiple)
        {
            SphereCalculations.TranslateInsideSphere(latlon, C, P1, radius, radius2, ColorMode.defaultMode, false, false);

            for (float lat = 0; lat < steps.x; lat += Mathf.PI * 2 / steps.x)
            {
                for (float lon = 0; lon < steps.y; lon += Mathf.PI / steps.y)
                {
                    ColorMode mode1 = ColorMode.defaultMode;
                    ColorMode mode2 = ColorMode.defaultMode;

                    mode2.P3 = Color.black;
                    mode2.P4 = Color.black;

                    SphereCalculations.TranslateInsideSphere(new Vector2(lat, lon), C, P1, radius, radius2, mode1, false, true, false);
                    SphereCalculations.TranslateInsideSphere(new Vector2(lat, lon), C, P1, radius, radius2 + (Mathf.Sin(lat * lon) / 10), mode2, false, true, false);
                }
            }
        }
    }
}

namespace ProjectionUtility
{
    public static class Conversion
    {
        public static Vector2 ToLatLon(this Vector3 vector, float sphere_radius = 1)
        {
            return new Vector2(
                Mathf.Acos(vector.y / sphere_radius),
                Mathf.Atan2(vector.x, vector.z)
            );
        }

        public static Vector3 ToVector(this Vector2 latlon, float sphere_radius = 1)
        {
            return new Vector3(
                 sphere_radius * Mathf.Sin(latlon.y) * Mathf.Sin(latlon.x),
                 sphere_radius * Mathf.Cos(latlon.x),
                 sphere_radius * Mathf.Cos(latlon.y) * Mathf.Sin(latlon.x)
            );
        }

        public static Vector2 NormalizedToLatLon(this Vector2 normalized)
        {
            return new Vector2(
                normalized.x * Mathf.PI,
                normalized.y * (Mathf.PI * 2)
            );
        }

        public static Vector2 LatLonToNormalized(this Vector2 latlon)
        {
            return new Vector2(
                latlon.x / Mathf.PI,
                latlon.y / (Mathf.PI * 2)
            );
        }
    }

    public static class SphereCalculations
    {
        public struct ColorMode
        {
            public Color C;
            public Color SC;
            public Color P1;
            public Color SP1;
            public Color P2;
            public Color P3;
            public Color P4;
            public Color R_C_UP;

            public static ColorMode defaultMode
            {
                get
                {
                    ColorMode defaultColorMode;

                    defaultColorMode.C = Color.yellow;
                    defaultColorMode.SC = Color.white;
                    defaultColorMode.P1 = Color.green;
                    defaultColorMode.SP1 = Color.cyan;
                    defaultColorMode.P2 = Color.cyan;
                    defaultColorMode.P3 = Color.magenta;
                    defaultColorMode.P4 = Color.magenta;
                    defaultColorMode.R_C_UP = Color.blue;

                    return defaultColorMode;
                }
            }
        }


        public static Vector2 TranslateSimple(Vector2 latlon, Vector3 P1, float radius, ColorMode colorMode)
        {
            Vector3 P2 = latlon.NormalizedToLatLon().ToVector(radius);
            Vector3 P3 = P2 - P1;

            float d2 = Mathf.Sqrt(Mathf.Pow(P3.x, 2) + Mathf.Pow(P3.y, 2) + Mathf.Pow(P3.z, 2));

            Gizmos.DrawSphere(Vector3.zero, radius);

            Gizmos.DrawLine(Vector3.zero, P3);
            Gizmos.DrawLine(P1, P2);

            Gizmos.DrawLine(Vector3.zero, new Vector3(0, 0, P1.z));
            Gizmos.DrawLine(P1, new Vector3(0, 0, P1.z));

            Gizmos.DrawLine(P2, new Vector3(P2.x, P3.y, P2.z));
            Gizmos.DrawLine(P3, new Vector3(P2.x, P3.y, P2.z));

            return P2.ToLatLon(d2).LatLonToNormalized();
        }

        public static Vector2 TranslateInsideSphere(Vector2 latlon, Vector3 C, Vector3 P1, float radius1, float radius2, ColorMode colorMode, bool drawLines = true, bool drawPoints = true, bool drawSpheres = true, float point_radius = 0.05f)
        {
            // 1. To point on sphere
            Vector3 P2 = latlon.NormalizedToLatLon().ToVector(radius1);

            // 2. P3 and P4
            float t1, t2;
            int isintersecting = RaySphereIntersection(P1, P2, C, radius2, out t1, out t2);

            Vector3 P3 = P1 + new Vector3(P2.x * t1, P2.y * t1, P2.z * t1);
            Vector3 P4 = P1 + new Vector3(P2.x * t2, P2.y * t2, P2.z * t2);

            // 3. Get latlon2
            Vector2 outlatlon = P4.ToLatLon(radius2).LatLonToNormalized();

            // Demo
            Ray R_P1_P2 = new Ray(P1, P2);
            Ray R_C_P3 = new Ray(C, P3);
            Ray R_C_P4 = new Ray(C, P4);
            Ray R_C_UP = new Ray(C, Vector3.up);


            // Draw outer spheres
            if (drawSpheres)
            {
                Gizmos.color = colorMode.SC;
                Gizmos.DrawSphere(C, radius2);
                Gizmos.color = colorMode.SP1;
                Gizmos.DrawSphere(P1, radius1);
                Gizmos.color = colorMode.SC;
                Gizmos.DrawWireSphere(C, radius2);
                Gizmos.color = colorMode.SP1;
                Gizmos.DrawWireSphere(P1, radius1);
            }

            // Draw lines
            if (drawLines)
            {
                Gizmos.color = colorMode.P1;
                Gizmos.DrawRay(R_P1_P2);
                if (isintersecting != 0)
                {
                    Gizmos.color = colorMode.C;
                    Gizmos.DrawRay(R_C_P3);
                    Gizmos.DrawRay(R_C_P4);
                }
                Gizmos.color = colorMode.R_C_UP;
                Gizmos.DrawRay(R_C_UP);
            }

            // Draw points
            if (drawPoints)
            {
                Gizmos.color = colorMode.C;
                Gizmos.DrawSphere(C, point_radius);
                Gizmos.color = colorMode.P1;
                Gizmos.DrawSphere(P1, point_radius);
                
                Gizmos.color = colorMode.P2;
                Gizmos.DrawSphere(P1 + P2, point_radius);
                
                if (isintersecting != 0)
                {
                    Gizmos.color = colorMode.P3;
                    Gizmos.DrawSphere(P3, point_radius);
                    Gizmos.color = colorMode.P4;
                    Gizmos.DrawSphere(P4, point_radius);
                }
            }
            
            return outlatlon;
        }

        public static int RaySphereIntersection(
           Vector3 rayPos, Vector3 rayDir,
           Vector3 spherePos, float sphereRadius,
           out float dist1, out float dist2)
        {
            dist1 = 0; dist2 = 0;

            Vector3 o_minus_c = rayPos - spherePos;

            float p = Vector3.Dot(rayDir, o_minus_c);
            float q = Vector3.Dot(o_minus_c, o_minus_c) - (sphereRadius * sphereRadius);

            float discriminant = (p * p) - q;
            if (discriminant < 0.0f)
            {
                return 0;
            }

            float dRoot = Mathf.Sqrt(discriminant);
            dist1 = -p - dRoot;
            dist2 = -p + dRoot;

            return (discriminant > 1e-7) ? 2 : 1;
        }
    }
}