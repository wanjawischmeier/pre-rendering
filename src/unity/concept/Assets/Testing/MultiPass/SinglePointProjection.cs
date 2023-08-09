using UnityEngine;

[ExecuteInEditMode]
public class SinglePointProjection : MonoBehaviour
{
    public Transform point;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void OnDrawGizmos()
    {
        Matrix4x4 MVP = GL.GetGPUProjectionMatrix(mainCamera.projectionMatrix, true) * mainCamera.worldToCameraMatrix;
        Vector4 p = new Vector4(point.position.x, point.position.y, point.position.z, 1);

        p = MVP * p;
        p.x = (p.x / p.w) * 0.5f + 0.5f;
        p.y = (p.y / p.w) * 0.5f + 0.5f;
        p.z = p.w;

        if (p.w > 0)
        {
            Gizmos.DrawSphere(p, 0.05f);
        }
    }
}
