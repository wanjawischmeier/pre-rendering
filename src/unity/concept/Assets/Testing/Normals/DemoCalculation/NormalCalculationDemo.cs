using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NormalCalculationDemo : MonoBehaviour
{
    public float pointRadius = 0.05f;

    private Vector3 p0, p1, p2, p3, p4;

    private void OnDrawGizmos()
    {
        p0 = GameObject.Find("P0").transform.position;
        p1 = GameObject.Find("P1").transform.position;
        p2 = GameObject.Find("P2").transform.position;
        p3 = GameObject.Find("P3").transform.position;
        p4 = GameObject.Find("P4").transform.position;

        Vector3 n0 = Vector3.Cross(p1 - p0, p2 - p0);
        Vector3 n1 = Vector3.Cross(p2 - p0, p3 - p0);
        Vector3 n2 = Vector3.Cross(p3 - p0, p4 - p0);
        Vector3 n3 = Vector3.Cross(p4 - p0, p1 - p0);
        Vector3 n = (n0 + n1 + n2 + n3).normalized;

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(p0, pointRadius);
        Gizmos.DrawSphere(p1, pointRadius);
        Gizmos.DrawSphere(p2, pointRadius);
        Gizmos.DrawSphere(p3, pointRadius);
        Gizmos.DrawSphere(p4, pointRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(p0, p1);
        Gizmos.DrawLine(p0, p2);
        Gizmos.DrawLine(p0, p3);
        Gizmos.DrawLine(p0, p4);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
        Gizmos.DrawLine(p1 - p0, p2 - p0);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(p0, n0);
        Gizmos.DrawLine(p0, n1);
        Gizmos.DrawLine(p0, n2);
        Gizmos.DrawLine(p0, n3);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(p0, n);
    }
}
