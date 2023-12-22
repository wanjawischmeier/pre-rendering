using UnityEngine;

[ExecuteInEditMode]
public class CubemapFrustumCullingDemo : MonoBehaviour
{
    [System.Serializable]
    public struct Frustum
    {
        public float nearClipPlane, farClipPlane, fieldOfView, aspectRatio;
        public Vector3 position, rotation;

        public Matrix4x4 matrix
        {
            get
            {
                Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.Euler(rotation), Vector3.one);
                matrix = Matrix4x4.Perspective(fieldOfView, aspectRatio, nearClipPlane, farClipPlane) * matrix;
                // matrix.SetColumn(3, new Vector4(position.x, position.y, position.z, 1));
                return matrix;
            }
        }

        public Bounds bounds
        {
            get
            {
                float scale = (farClipPlane - nearClipPlane);
                return new Bounds(position + Vector3.forward * (nearClipPlane + scale / 2), boundScale * scale);
            }
        }
    }

    private Vector3[] cubemapFaceAllignment = new Vector3[6]
    {
        Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
    };

    public float cubemapNearClipPlane, cubemapFarClipPlane;
    public Vector3[] cubemapPositions;
    public Vector3 debugScale, boundsPosition, boundsScale, _boundScale;
    public static Vector3 boundScale;
    private Frustum cameraFrustum, testFrustum;
    private Frustum[] cubemapFrustums;

    private void Start()
    {
        Camera currentCamera = GetComponent<Camera>();
        cameraFrustum = new Frustum
        {
            nearClipPlane = currentCamera.nearClipPlane,
            farClipPlane = currentCamera.farClipPlane,
            fieldOfView = currentCamera.fieldOfView,
            aspectRatio = currentCamera.aspect,
            position = transform.position,
            rotation = transform.rotation.eulerAngles
        };
        
        testFrustum = new Frustum
        {
            nearClipPlane = cubemapNearClipPlane,
            farClipPlane = cubemapFarClipPlane,
            fieldOfView = 90,
            aspectRatio = 1,
            position = Vector3.zero,
            rotation = Vector3.zero
        };
        
        cubemapFrustums = new Frustum[cubemapPositions.Length];
        for (int cubemapIndex = 0; cubemapIndex < cubemapPositions.Length; cubemapIndex++)
        {
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                cubemapFrustums[cubemapIndex] = new Frustum
                {
                    nearClipPlane = cubemapNearClipPlane,
                    farClipPlane = cubemapFarClipPlane,
                    fieldOfView = 90, aspectRatio = 1,
                    position = cubemapPositions[cubemapIndex],
                    rotation = cubemapFaceAllignment[faceIndex]
                };
            }
        }
    }

    private void Update()
    {
        boundScale = _boundScale;

        cameraFrustum.position = transform.position;
        cameraFrustum.rotation = transform.rotation.eulerAngles;

        testFrustum.nearClipPlane = cubemapNearClipPlane;
        testFrustum.farClipPlane = cubemapFarClipPlane;
    }

    private void OnDrawGizmos()
    {
        Debug.Log($"Camera Frustum Matrix:\n{cameraFrustum.matrix}");
        Debug.Log($"Test Frustum Matrix:\n{testFrustum.matrix}");
        bool frustumsIntersect = AreFrustumsIntersecting(cameraFrustum, testFrustum);
        // GeometryUtility.Te
        Gizmos.color = frustumsIntersect ? Color.red : Color.green;
        Gizmos.DrawFrustum(Vector3.zero, 90, cubemapFarClipPlane, cubemapNearClipPlane, 1);
        Gizmos.DrawCube(testFrustum.bounds.center, testFrustum.bounds.size);
        boundsPosition = testFrustum.bounds.center;
        boundsScale = testFrustum.bounds.size;
    }

    // Function to check frustum intersection using matrices
    public bool AreFrustumsIntersecting(Frustum frustum0, Frustum frustum1)
    {
        // Use matrix operations to check for intersection
        Matrix4x4 relativeMatrix = frustum1.matrix.inverse * frustum0.matrix;
        Debug.Log($"Relative Matrix:\n{relativeMatrix}");

        // Check if the relative matrix has any scaling, which indicates intersection
        if (HasScaling(relativeMatrix))
        {
            return true; // Frustums are intersecting
        }

        return false; // Frustums are not intersecting
    }

    // Function to check if a matrix has scaling
    private bool HasScaling(Matrix4x4 matrix)
    {
        Vector3 scale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
        debugScale = scale;
        return Mathf.Abs(scale.x - 1) > float.Epsilon || Mathf.Abs(scale.y - 1) > float.Epsilon || Mathf.Abs(scale.z - 1) > float.Epsilon;
    }

    bool CheckFrustumIntersection(Frustum frustum1, Frustum frustum2)
    {
        // Calculate the planes of the frustums
        Plane[] planes1 = GeometryUtility.CalculateFrustumPlanes(frustum1.matrix);
        Plane[] planes2 = GeometryUtility.CalculateFrustumPlanes(frustum2.matrix);
        
        // Check if the frustums intersect
        if (GeometryUtility.TestPlanesAABB(planes1, frustum2.bounds) || GeometryUtility.TestPlanesAABB(planes2, frustum1.bounds))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
