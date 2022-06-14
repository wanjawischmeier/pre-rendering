using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoNormalProcess : MonoBehaviour
{
    [System.Serializable]
    public struct NormalCalculationDepths
    {
        public float CD, TD, BD, LD, RD;
    }

    public Vector2Int latlon;
    public NormalCalculationDepths depths;
    public float eps = 0.5f;
    public float pointSize = 0.5f;


    private Vector2 VEC_DEG2RAD = new Vector2(Mathf.Deg2Rad, Mathf.Deg2Rad);

    private Vector3 latLonToVec(Vector2 latlon, float depth)
    {
        return new Vector3(
            depth * Mathf.Sin(latlon.y) * Mathf.Sin(latlon.x),
            depth * Mathf.Cos(latlon.x),
            depth * Mathf.Cos(latlon.y) * Mathf.Sin(latlon.x)
        );
    }

    private void Start()
    {
        
    }

    private void OnDrawGizmos()
    {
        Vector2 cll = latlon * VEC_DEG2RAD;
        Vector2 tll = new Vector2(cll.x - eps, cll.y);
        Vector2 bll = new Vector2(cll.x + eps, cll.y);
        Vector2 lll = new Vector2(cll.x, cll.y - eps);
        Vector2 rll = new Vector2(cll.x, cll.y + eps);

        Vector3 cp = latLonToVec(cll, depths.CD);
        Vector3 tp = latLonToVec(tll, depths.TD);
        Vector3 bp = latLonToVec(bll, depths.BD);
        Vector3 lp = latLonToVec(lll, depths.LD);
        Vector3 rp = latLonToVec(rll, depths.RD);

        Gizmos.DrawLine(Vector3.zero, cp);

        Gizmos.DrawSphere(Vector3.zero, pointSize);
        Gizmos.DrawSphere(cp, pointSize);
        Gizmos.DrawSphere(tp, pointSize / 2);
        Gizmos.DrawSphere(bp, pointSize / 2);
        Gizmos.DrawSphere(lp, pointSize / 2);
        Gizmos.DrawSphere(rp, pointSize / 2);
    }
}
