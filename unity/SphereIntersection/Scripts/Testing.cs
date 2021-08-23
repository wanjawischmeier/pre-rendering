using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Testing : MonoBehaviour
{
    public Transform empty;
    public Vector3 rayOrigin;
    public Vector3 rayDirection;
    public Vector3 sphereOrigin;
    public float sphereRadius;

    public Vector3 input_vector;
    public Vector2 converted;
    public Vector3 reconverted;

    void Start()
    {
        
    }

    void Update()
    {


    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(sphereOrigin, sphereRadius);

        Ray ray = new Ray(rayOrigin, empty.position);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(ray);

        float t1, t2;
        RaySphereIntersection(rayOrigin, rayDirection.normalized, sphereOrigin, sphereRadius, out t1, out t2);
        if (true)
        {
            Vector3 pos1 = ray.GetPoint(t1);
            Vector3 pos2 = ray.GetPoint(t2);
            Vector2 tmp = pos2.ToLatLon(sphereRadius);
            Vector3 pos_conv = tmp.ToVector(sphereRadius);

            input_vector = pos2;
            converted = tmp * Mathf.Rad2Deg;
            reconverted = pos_conv;

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(pos1, 0.025f);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(pos2, 0.05f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pos_conv, 0.025f);
        }
    }

    int RaySphereIntersection(
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

public static class ConversionUtility
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
}