using UnityEngine;

[ExecuteInEditMode]
public class NormalPlaneDemo : MonoBehaviour
{
    public Vector3 offset = new Vector3(0.5f, 0.25f, 0.5f);
    public Vector3 pointRight = new Vector3(0.75f, 0.75f, 0.25f);
    public Vector3 pointBottom = new Vector3(0.75f, 0.75f, 0.25f);
    public Vector3 demoPlaneAngle = new Vector3(-45, 10, 0);
    [Range(0, Mathf.PI)]
    public float latitude = 0.75f;
    [Range(0, Mathf.PI * 2)]
    public float longitude = 0.75f;
    public float distance = 1.5f;
    public float pointRadius = 0.05f;

    private void OnDrawGizmos()
    {
        Vector3 point = new Vector3(
            distance * Mathf.Sin(longitude) * Mathf.Sin(latitude),
            distance * Mathf.Cos(latitude),
            distance * Mathf.Cos(longitude) * Mathf.Sin(latitude)
        );
        Vector3 normal = Vector3.Cross(pointRight - point, pointBottom - point);
        Vector3 intersection = RayPlaneIntersection(offset, point, point, normal);

        // Demo Plane
        Gizmos.matrix = Matrix4x4.TRS(point, Quaternion.Euler(demoPlaneAngle), Vector3.one);
        Gizmos.DrawCube(Vector3.zero, new Vector3(1, 1, 0.0001f));
        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.DrawLine(Vector3.zero, offset);
        Gizmos.DrawLine(offset, point + offset);
        Gizmos.DrawLine(point, point + normal * 10);
        Gizmos.DrawSphere(Vector3.zero, pointRadius);
        Gizmos.DrawSphere(offset, pointRadius);
        Gizmos.DrawSphere(point, pointRadius);
        Gizmos.DrawSphere(pointRight, pointRadius);
        Gizmos.DrawSphere(pointBottom, pointRadius);
        Gizmos.DrawSphere(intersection, pointRadius);
    }

    // From this thread: https://discourse.vvvv.org/t/infinite-ray-intersects-with-infinite-plane/10537
    private Vector3 RayPlaneIntersection(Vector3 rayOrigin, Vector3 rayDirection, Vector3 planeOrigin, Vector3 planeNormal)
    {
        float rDotn = Vector3.Dot(rayDirection, planeNormal);
        float s = Vector3.Dot(planeNormal, (planeOrigin - rayOrigin)) / rDotn;
        return rayOrigin + s * rayDirection;
    }
}
