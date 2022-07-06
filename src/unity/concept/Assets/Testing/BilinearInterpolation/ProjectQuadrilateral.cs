using UnityEngine;

public class ProjectQuadrilateral : MonoBehaviour
{
    public float pointRadius = 0.05f;

    private Transform q00, q10, q01, q11, tgt;

    private void Start()
    {
        q00 = GameObject.Find("q00").transform;
        q10 = GameObject.Find("q10").transform;
        q01 = GameObject.Find("q01").transform;
        q11 = GameObject.Find("q11").transform;
        tgt = GameObject.Find("tgt").transform;
    }

    // private Vector2 ProjectToUnitSquare()

    private void OnDrawGizmos()
    {
        if (tgt == null) Start();
        
        Gizmos.DrawSphere(q00.position, pointRadius);
        Gizmos.DrawSphere(q10.position, pointRadius);
        Gizmos.DrawSphere(q01.position, pointRadius);
        Gizmos.DrawSphere(q11.position, pointRadius);
        Gizmos.DrawSphere(tgt.position, pointRadius);
    }
}
