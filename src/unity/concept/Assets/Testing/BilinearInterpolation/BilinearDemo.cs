using UnityEngine;

public class BilinearDemo : MonoBehaviour
{
    public Color c00, c10, c01, c11;
    public float pointRadius = 0.05f;
    public Material material;

    private Transform q00, q10, q01, q11;

    private void Start()
    {
        q00 = GameObject.Find("q00").transform;
        q10 = GameObject.Find("q10").transform;
        q01 = GameObject.Find("q01").transform;
        q11 = GameObject.Find("q11").transform;
    }

    private void OnDrawGizmos()
    {
        if (q00 == null) Start();

        material.SetVector("_Q00", q00.position);
        material.SetVector("_Q10", q10.position);
        material.SetVector("_Q01", q01.position);
        material.SetVector("_Q11", q11.position);
        material.SetColor("_C00", c00);
        material.SetColor("_C10", c10);
        material.SetColor("_C01", c01);
        material.SetColor("_C11", c11);

        Gizmos.color = c00;
        Gizmos.DrawSphere(q00.position, pointRadius);
        Gizmos.color = c10;
        Gizmos.DrawSphere(q10.position, pointRadius);
        Gizmos.color = c01;
        Gizmos.DrawSphere(q01.position, pointRadius);
        Gizmos.color = c11;
        Gizmos.DrawSphere(q11.position, pointRadius);
    }
}
