using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AABBIntersectionDemo : MonoBehaviour
{
    public GameObject rayObj, volumeObj;
    public float itmin, itmax;
    CRay ray;
    AABB aabb;

    struct CRay
    {
        public Vector3 position, direction;
    }

    struct AABB
    {
        public Vector3 min, max;
    }

    private void Start()
    {
        ray = new CRay();
        aabb = new AABB();
    }

    private void OnDrawGizmos()
    {
        ray.position = rayObj.transform.position;
        ray.direction = rayObj.transform.eulerAngles;

        aabb.min = volumeObj.transform.position - volumeObj.transform.lossyScale / 2.0f;
        aabb.max = volumeObj.transform.position + volumeObj.transform.lossyScale / 2.0f;

        Gizmos.color = new Color(0, 0, 0, 0.5f);
        Gizmos.DrawCube(volumeObj.transform.position, volumeObj.transform.lossyScale);

        float tmin = float.NegativeInfinity;
        float tmax = float.PositiveInfinity;

        if (RayAABBIntersection(ray, aabb, ref tmin, ref tmax))
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(ray.position + ray.direction * tmin, 0.05f);
            Gizmos.DrawSphere(ray.position + ray.direction * tmax, 0.05f);
        }
        else
            Gizmos.color = Color.gray;

        Gizmos.DrawSphere(ray.position, 0.05f);
        Gizmos.DrawRay(ray.position, ray.direction);
    }

    bool RayAABBIntersection(CRay ray, AABB aabb, ref float tmin, ref float tmax)
    {
        Vector3 invD = Foreach(ray.direction, v => 1.0f / v);
        Vector3 t0s = Foreach(aabb.min - ray.position, invD, (a, b) => a * b);
        Vector3 t1s = Foreach(aabb.max - ray.position, invD, (a, b) => a * b);

        Vector3 tsmaller = Foreach(t0s, t1s, (a, b) => Mathf.Min(a, b));
        Vector3 tbigger = Foreach(t0s, t1s, (a, b) => Mathf.Max(a, b));

        tmin = Mathf.Max(tmin, Mathf.Max(tsmaller[0], Mathf.Max(tsmaller[1], tsmaller[2])));
        tmax = Mathf.Min(tmax, Mathf.Min(tbigger[0], Mathf.Min(tbigger[1], tbigger[2])));

        return (tmin < tmax);
    }

    Vector3 Foreach(Vector3 a, Func<float, float> func) => new Vector3(
        func(a.x),
        func(a.y),
        func(a.z));

    Vector3 Foreach(Vector3 a, Vector3 b, Func<float, float, float> func) => new Vector3(
        func(a.x, b.x),
        func(a.y, b.y),
        func(a.z, b.z));
}
