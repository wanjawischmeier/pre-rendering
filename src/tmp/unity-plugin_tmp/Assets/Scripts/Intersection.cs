using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Intersection : MonoBehaviour
{
    // public Transform empty;
    public Vector3 rayOrigin;
    public Vector3 rayDirection;
    public Vector3 sphereOrigin;
    public float sphereRadius;

    public float debug;
    public Vector2 output;
    public Vector3 L_d;
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
        Ray ray = new Ray(rayOrigin, rayDirection.normalized);
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
            
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(pos1, 0.05f);
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pos2, 0.05f);
            output = new Vector2(t1, t2);
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