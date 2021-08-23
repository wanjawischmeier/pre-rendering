using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Intersection : MonoBehaviour
{
    public Transform empty;
    public Vector3 rayOrigin;
    public Vector3 rayDirection;
    public Vector3 sphereOrigin;
    public float sphereRadius;

    public float debug;
    public Vector2 output;
    public Vector3 L_d;
    public Vector2 tmp;
    public Vector2 ll;
    public float tc_d;
    public float d_d;
    public float t1c_d;

    void Start()
    {
        Debug.Log(transform.up);
    }

    void Update()
    {


    }
    private void OnDrawGizmos()
    {
        Ray ray = new Ray(rayOrigin, empty.position);
        float t1, t2;

        Gizmos.DrawSphere(sphereOrigin, sphereRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(ray);
        // IntersectRaySphere(rayOrigin, rayDirection, sphereOrigin, sphereRadius, out t1, out t2);
        debug = RaySphereIntersection(rayOrigin, rayDirection.normalized, sphereOrigin, sphereRadius, out t1, out t2);
        if (true)
        {
            Vector3 pos1 = ray.GetPoint(t1);
            Vector3 pos2 = ray.GetPoint(t2);
            // Vector2 tmp = pos2.normalized.ToLatLon(sphereRadius);
            Vector3 pos_conv = tmp.ToVector2(sphereRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(pos2, 0.05f);/*
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pos1, 0.05f);*/

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(pos_conv, 0.05f);
            output = new Vector2(t1, t2);
            // ll = tmp;
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
    public static Vector2 ToLatLon(this Vector3 vector, float sphere_radius)
    {
        return new Vector2(
            90 - (Mathf.Acos(vector.y / sphere_radius)) * 180 / Mathf.PI,
            ((270 + (Mathf.Atan2(vector.x, vector.z)) * 180 / Mathf.PI) % 360) - 180 // Or -360
        );
    }

    public static Vector3 ToVector(this Vector2 latlon, float sphere_radius)
    {
        return new Vector3(
             sphere_radius * Mathf.Cos(latlon.y) * Mathf.Sin(latlon.x),
             sphere_radius * Mathf.Sin(latlon.y) * Mathf.Sin(latlon.x),
             sphere_radius * Mathf.Cos(latlon.x)
        );
    }

    public static Vector3 ToVector2(this Vector2 latlon, float sphere_radius)
    {
        float latitude_rad = (latlon.x) / 57.25f; // * Mathf.PI / 180;
        float longitude_rad = (latlon.y); // * Mathf.PI / 180;

        return new Vector3(
             sphere_radius * Mathf.Cos(latitude_rad) * Mathf.Cos(longitude_rad),
             -sphere_radius * Mathf.Cos(latitude_rad) * Mathf.Sin(longitude_rad),
             sphere_radius * Mathf.Sin(latitude_rad)
        );
    }
}